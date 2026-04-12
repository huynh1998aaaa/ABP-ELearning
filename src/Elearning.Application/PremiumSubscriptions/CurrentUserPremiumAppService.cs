using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using Volo.Abp.Domain.Repositories;

namespace Elearning.PremiumSubscriptions;

[Authorize]
public class CurrentUserPremiumAppService : ElearningAppService, ICurrentUserPremiumAppService
{
    private readonly IRepository<PremiumPlan, Guid> _premiumPlanRepository;
    private readonly IRepository<UserPremiumSubscription, Guid> _subscriptionRepository;

    public CurrentUserPremiumAppService(
        IRepository<UserPremiumSubscription, Guid> subscriptionRepository,
        IRepository<PremiumPlan, Guid> premiumPlanRepository)
    {
        _subscriptionRepository = subscriptionRepository;
        _premiumPlanRepository = premiumPlanRepository;
    }

    public async Task<PremiumStatusDto> GetCurrentUserPremiumStatusAsync()
    {
        if (!CurrentUser.Id.HasValue)
        {
            return new PremiumStatusDto
            {
                IsPremium = false
            };
        }

        var now = Clock.Now;
        var query = await _subscriptionRepository.GetQueryableAsync();
        var subscription = await AsyncExecuter.FirstOrDefaultAsync(query
            .Where(x =>
                x.UserId == CurrentUser.Id.Value &&
                x.Status == PremiumSubscriptionStatus.Active &&
                x.StartTime <= now &&
                x.EndTime > now)
            .OrderByDescending(x => x.EndTime));

        if (subscription == null)
        {
            return new PremiumStatusDto
            {
                IsPremium = false
            };
        }

        var plan = await _premiumPlanRepository.FindAsync(subscription.PremiumPlanId);

        return new PremiumStatusDto
        {
            IsPremium = true,
            SubscriptionId = subscription.Id,
            PremiumPlanId = subscription.PremiumPlanId,
            PremiumPlanName = plan?.DisplayName ?? string.Empty,
            ActivationNumber = subscription.ActivationNumber,
            ActivatedTime = subscription.ActivatedTime,
            StartTime = subscription.StartTime,
            EndTime = subscription.EndTime,
            Status = subscription.Status,
            RemainingDays = Math.Max(0, (int)Math.Ceiling((subscription.EndTime - now).TotalDays))
        };
    }
}
