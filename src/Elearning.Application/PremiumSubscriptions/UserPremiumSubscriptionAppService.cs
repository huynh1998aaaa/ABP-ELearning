using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elearning.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Identity;

namespace Elearning.PremiumSubscriptions;

[Authorize(ElearningPermissions.PremiumSubscriptions.Default)]
public class UserPremiumSubscriptionAppService : ElearningAppService, IUserPremiumSubscriptionAppService
{
    private readonly IGuidGenerator _guidGenerator;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IRepository<PremiumPlan, Guid> _premiumPlanRepository;
    private readonly IRepository<UserPremiumSubscription, Guid> _subscriptionRepository;

    public UserPremiumSubscriptionAppService(
        IRepository<UserPremiumSubscription, Guid> subscriptionRepository,
        IRepository<PremiumPlan, Guid> premiumPlanRepository,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IGuidGenerator guidGenerator)
    {
        _subscriptionRepository = subscriptionRepository;
        _premiumPlanRepository = premiumPlanRepository;
        _identityUserRepository = identityUserRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task<PagedResultDto<UserPremiumSubscriptionDto>> GetListAsync(GetUserPremiumSubscriptionListInput input)
    {
        var now = Clock.Now;
        var query = await _subscriptionRepository.GetQueryableAsync();

        if (input.Status.HasValue)
        {
            query = input.Status.Value == PremiumSubscriptionStatus.Expired
                ? query.Where(x => x.Status == PremiumSubscriptionStatus.Expired || (x.Status == PremiumSubscriptionStatus.Active && x.EndTime <= now))
                : query.Where(x => x.Status == input.Status.Value);
        }

        if (input.PremiumPlanId.HasValue)
        {
            query = query.Where(x => x.PremiumPlanId == input.PremiumPlanId.Value);
        }

        if (!input.Filter.IsNullOrWhiteSpace())
        {
            var userIds = await GetFilteredUserIdsAsync(input.Filter!);
            if (userIds.Count == 0)
            {
                return new PagedResultDto<UserPremiumSubscriptionDto>(0, Array.Empty<UserPremiumSubscriptionDto>());
            }

            query = query.Where(x => userIds.Contains(x.UserId));
        }

        var totalCount = await AsyncExecuter.CountAsync(query);
        var items = await AsyncExecuter.ToListAsync(ApplySorting(query, input.Sorting)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount));

        return new PagedResultDto<UserPremiumSubscriptionDto>(
            totalCount,
            await MapToDtosAsync(items, now));
    }

    public async Task<UserPremiumSubscriptionDto> GetAsync(Guid id)
    {
        var subscription = await _subscriptionRepository.GetAsync(id);
        return (await MapToDtosAsync(new[] { subscription }, Clock.Now)).Single();
    }

    public async Task<UserPremiumSubscriptionDto?> GetActiveByUserAsync(Guid userId)
    {
        var now = Clock.Now;
        var query = await _subscriptionRepository.GetQueryableAsync();
        var subscription = await AsyncExecuter.FirstOrDefaultAsync(query
            .Where(x => x.UserId == userId && x.Status == PremiumSubscriptionStatus.Active && x.StartTime <= now && x.EndTime > now)
            .OrderByDescending(x => x.EndTime));

        if (subscription == null)
        {
            return null;
        }

        return (await MapToDtosAsync(new[] { subscription }, now)).Single();
    }

    public async Task<PremiumStatusDto> GetUserPremiumStatusAsync(Guid userId)
    {
        var activeSubscription = await GetActiveByUserAsync(userId);
        if (activeSubscription == null)
        {
            return new PremiumStatusDto
            {
                IsPremium = false
            };
        }

        return new PremiumStatusDto
        {
            IsPremium = true,
            SubscriptionId = activeSubscription.Id,
            PremiumPlanId = activeSubscription.PremiumPlanId,
            PremiumPlanName = activeSubscription.PremiumPlanName,
            ActivationNumber = activeSubscription.ActivationNumber,
            ActivatedTime = activeSubscription.ActivatedTime,
            StartTime = activeSubscription.StartTime,
            EndTime = activeSubscription.EndTime,
            Status = activeSubscription.Status,
            RemainingDays = activeSubscription.RemainingDays
        };
    }

    [Authorize(ElearningPermissions.PremiumSubscriptions.Create)]
    public async Task<UserPremiumSubscriptionDto> CreateAsync(CreateUserPremiumSubscriptionDto input)
    {
        var user = await _identityUserRepository.FindAsync(input.UserId);
        if (user == null)
        {
            throw new EntityNotFoundException(typeof(IdentityUser), input.UserId);
        }

        var plan = await _premiumPlanRepository.GetAsync(input.PremiumPlanId);
        if (!plan.IsActive)
        {
            throw new BusinessException(ElearningDomainErrorCodes.InactivePremiumPlanCannotBeUsed)
                .WithData(nameof(PremiumPlan.Id), input.PremiumPlanId);
        }

        var now = Clock.Now;
        await EnsureUserHasNoActivePremiumAsync(input.UserId, now);

        var activationNumber = await GetNextActivationNumberAsync(input.UserId);
        var subscription = new UserPremiumSubscription(
            _guidGenerator.Create(),
            input.UserId,
            input.PremiumPlanId,
            activationNumber,
            now,
            plan.DurationMonths,
            input.Note);

        await _subscriptionRepository.InsertAsync(subscription, autoSave: true);
        return (await MapToDtosAsync(new[] { subscription }, now)).Single();
    }

    [Authorize(ElearningPermissions.PremiumSubscriptions.Update)]
    public async Task<UserPremiumSubscriptionDto> ExtendAsync(Guid id)
    {
        var subscription = await _subscriptionRepository.GetAsync(id);
        var plan = await _premiumPlanRepository.GetAsync(subscription.PremiumPlanId);

        subscription.Extend(plan.DurationMonths);
        await _subscriptionRepository.UpdateAsync(subscription, autoSave: true);

        return await GetAsync(id);
    }

    [Authorize(ElearningPermissions.PremiumSubscriptions.Cancel)]
    public async Task CancelAsync(Guid id, CancelPremiumSubscriptionDto input)
    {
        var subscription = await _subscriptionRepository.GetAsync(id);
        subscription.Cancel(Clock.Now, input.Reason);
        await _subscriptionRepository.UpdateAsync(subscription, autoSave: true);
    }

    private async Task EnsureUserHasNoActivePremiumAsync(Guid userId, DateTime now)
    {
        var query = await _subscriptionRepository.GetQueryableAsync();
        if (await AsyncExecuter.AnyAsync(query.Where(x =>
                x.UserId == userId &&
                x.Status == PremiumSubscriptionStatus.Active &&
                x.StartTime <= now &&
                x.EndTime > now)))
        {
            throw new BusinessException(ElearningDomainErrorCodes.UserAlreadyHasActivePremium)
                .WithData(nameof(UserPremiumSubscription.UserId), userId);
        }
    }

    private async Task<int> GetNextActivationNumberAsync(Guid userId)
    {
        var query = await _subscriptionRepository.GetQueryableAsync();
        var maxActivationNumber = await AsyncExecuter.ToListAsync(query
            .Where(x => x.UserId == userId)
            .Select(x => x.ActivationNumber));

        return maxActivationNumber.Count == 0 ? 1 : maxActivationNumber.Max() + 1;
    }

    private async Task<List<Guid>> GetFilteredUserIdsAsync(string filter)
    {
        var normalizedFilter = filter.Trim();
        var userQuery = await _identityUserRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(userQuery
            .Where(x =>
                x.UserName.Contains(normalizedFilter) ||
                x.Email.Contains(normalizedFilter) ||
                (x.Name != null && x.Name.Contains(normalizedFilter)) ||
                (x.Surname != null && x.Surname.Contains(normalizedFilter)))
            .Select(x => x.Id));
    }

    private async Task<List<UserPremiumSubscriptionDto>> MapToDtosAsync(IReadOnlyCollection<UserPremiumSubscription> subscriptions, DateTime now)
    {
        var userIds = subscriptions.Select(x => x.UserId).Distinct().ToList();
        var planIds = subscriptions.Select(x => x.PremiumPlanId).Distinct().ToList();

        var userQuery = await _identityUserRepository.GetQueryableAsync();
        var users = await AsyncExecuter.ToListAsync(userQuery.Where(x => userIds.Contains(x.Id)));
        var userMap = users.ToDictionary(x => x.Id);

        var planQuery = await _premiumPlanRepository.GetQueryableAsync();
        var plans = await AsyncExecuter.ToListAsync(planQuery.Where(x => planIds.Contains(x.Id)));
        var planMap = plans.ToDictionary(x => x.Id);

        return subscriptions.Select(x =>
        {
            userMap.TryGetValue(x.UserId, out var user);
            planMap.TryGetValue(x.PremiumPlanId, out var plan);
            var isCurrentlyActive = x.IsCurrentlyActive(now);

            return new UserPremiumSubscriptionDto
            {
                Id = x.Id,
                UserId = x.UserId,
                UserName = user?.UserName ?? string.Empty,
                Email = user?.Email ?? string.Empty,
                PremiumPlanId = x.PremiumPlanId,
                PremiumPlanCode = plan?.Code ?? string.Empty,
                PremiumPlanName = plan?.DisplayName ?? string.Empty,
                ActivationNumber = x.ActivationNumber,
                ActivatedTime = x.ActivatedTime,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                Status = isCurrentlyActive ? x.Status : x.Status == PremiumSubscriptionStatus.Active ? PremiumSubscriptionStatus.Expired : x.Status,
                IsCurrentlyActive = isCurrentlyActive,
                RemainingDays = isCurrentlyActive ? Math.Max(0, (int)Math.Ceiling((x.EndTime - now).TotalDays)) : 0,
                Note = x.Note,
                CancelledTime = x.CancelledTime,
                CancellationReason = x.CancellationReason,
                CreationTime = x.CreationTime,
                CreatorId = x.CreatorId,
                LastModificationTime = x.LastModificationTime,
                LastModifierId = x.LastModifierId,
                IsDeleted = x.IsDeleted,
                DeleterId = x.DeleterId,
                DeletionTime = x.DeletionTime
            };
        }).ToList();
    }

    private static IQueryable<UserPremiumSubscription> ApplySorting(IQueryable<UserPremiumSubscription> query, string? sorting)
    {
        return sorting?.Trim().ToLowerInvariant() switch
        {
            "activatedtime" => query.OrderBy(x => x.ActivatedTime),
            "activatedtime desc" => query.OrderByDescending(x => x.ActivatedTime),
            "endtime" => query.OrderBy(x => x.EndTime),
            "endtime desc" => query.OrderByDescending(x => x.EndTime),
            "activationnumber" => query.OrderBy(x => x.ActivationNumber),
            "activationnumber desc" => query.OrderByDescending(x => x.ActivationNumber),
            _ => query.OrderByDescending(x => x.ActivatedTime)
        };
    }
}
