using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elearning.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace Elearning.PremiumSubscriptions;

[Authorize(ElearningPermissions.PremiumSubscriptions.Default)]
public class PremiumPlanAppService : ElearningAppService, IPremiumPlanAppService
{
    private static readonly Regex CodeRegex = new("^[a-z0-9_-]+$", RegexOptions.Compiled);

    private readonly IGuidGenerator _guidGenerator;
    private readonly IRepository<PremiumPlan, Guid> _premiumPlanRepository;

    public PremiumPlanAppService(
        IRepository<PremiumPlan, Guid> premiumPlanRepository,
        IGuidGenerator guidGenerator)
    {
        _premiumPlanRepository = premiumPlanRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task<PagedResultDto<PremiumPlanDto>> GetListAsync(GetPremiumPlanListInput input)
    {
        var query = await _premiumPlanRepository.GetQueryableAsync();

        if (!input.Filter.IsNullOrWhiteSpace())
        {
            var filter = input.Filter!.Trim();
            query = query.Where(x =>
                x.Code.Contains(filter) ||
                x.DisplayName.Contains(filter) ||
                (x.Description != null && x.Description.Contains(filter)));
        }

        if (input.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == input.IsActive.Value);
        }

        var totalCount = await AsyncExecuter.CountAsync(query);
        var items = await AsyncExecuter.ToListAsync(ApplySorting(query, input.Sorting)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount));

        return new PagedResultDto<PremiumPlanDto>(
            totalCount,
            items.Select(MapToDto).ToList());
    }

    public async Task<PremiumPlanDto> GetAsync(Guid id)
    {
        return MapToDto(await _premiumPlanRepository.GetAsync(id));
    }

    public async Task<PremiumPlanDto> GetDefaultSixMonthsPlanAsync()
    {
        var plan = await _premiumPlanRepository.FindAsync(x => x.Code == PremiumPlanConsts.SixMonthsCode);
        if (plan == null)
        {
            throw new BusinessException(ElearningDomainErrorCodes.InactivePremiumPlanCannotBeUsed)
                .WithData(nameof(PremiumPlan.Code), PremiumPlanConsts.SixMonthsCode);
        }

        return MapToDto(plan);
    }

    [Authorize(ElearningPermissions.PremiumSubscriptions.Create)]
    public async Task<PremiumPlanDto> CreateAsync(CreatePremiumPlanDto input)
    {
        var code = NormalizeCode(input.Code);
        await ValidateCodeAsync(code);

        var plan = new PremiumPlan(
            _guidGenerator.Create(),
            code,
            input.DisplayName,
            input.PlanType,
            input.DurationMonths,
            input.Price,
            input.Currency,
            input.SortOrder,
            description: input.Description);

        if (!input.IsActive)
        {
            plan.Deactivate();
        }

        await _premiumPlanRepository.InsertAsync(plan, autoSave: true);
        return MapToDto(plan);
    }

    [Authorize(ElearningPermissions.PremiumSubscriptions.Update)]
    public async Task<PremiumPlanDto> UpdateAsync(Guid id, UpdatePremiumPlanDto input)
    {
        var plan = await _premiumPlanRepository.GetAsync(id);
        var code = NormalizeCode(input.Code);

        if (!string.Equals(plan.Code, code, StringComparison.Ordinal))
        {
            throw new BusinessException(ElearningDomainErrorCodes.PremiumPlanCodeCannotBeChanged)
                .WithData(nameof(PremiumPlan.Code), plan.Code);
        }

        plan.UpdateDetails(
            input.DisplayName,
            input.Description,
            input.PlanType,
            input.DurationMonths,
            input.Price,
            input.Currency,
            input.SortOrder);

        await _premiumPlanRepository.UpdateAsync(plan, autoSave: true);
        return MapToDto(plan);
    }

    [Authorize(ElearningPermissions.PremiumSubscriptions.Update)]
    public async Task ActivateAsync(Guid id)
    {
        var plan = await _premiumPlanRepository.GetAsync(id);
        plan.Activate();
        await _premiumPlanRepository.UpdateAsync(plan, autoSave: true);
    }

    [Authorize(ElearningPermissions.PremiumSubscriptions.Update)]
    public async Task DeactivateAsync(Guid id)
    {
        var plan = await _premiumPlanRepository.GetAsync(id);
        plan.Deactivate();
        await _premiumPlanRepository.UpdateAsync(plan, autoSave: true);
    }

    private async Task ValidateCodeAsync(string code)
    {
        if (!CodeRegex.IsMatch(code))
        {
            throw new BusinessException(ElearningDomainErrorCodes.InvalidQuestionTypeCode)
                .WithData(nameof(PremiumPlan.Code), code);
        }

        if (await _premiumPlanRepository.FindAsync(x => x.Code == code) != null)
        {
            throw new BusinessException(ElearningDomainErrorCodes.PremiumPlanCodeAlreadyExists)
                .WithData(nameof(PremiumPlan.Code), code);
        }
    }

    private static string NormalizeCode(string code)
    {
        return code.Trim().ToLowerInvariant();
    }

    private static IQueryable<PremiumPlan> ApplySorting(IQueryable<PremiumPlan> query, string? sorting)
    {
        return sorting?.Trim().ToLowerInvariant() switch
        {
            "code" => query.OrderBy(x => x.Code),
            "code desc" => query.OrderByDescending(x => x.Code),
            "displayname" => query.OrderBy(x => x.DisplayName),
            "displayname desc" => query.OrderByDescending(x => x.DisplayName),
            "sortorder desc" => query.OrderByDescending(x => x.SortOrder).ThenBy(x => x.DisplayName),
            _ => query.OrderBy(x => x.SortOrder).ThenBy(x => x.DisplayName)
        };
    }

    private static PremiumPlanDto MapToDto(PremiumPlan plan)
    {
        return new PremiumPlanDto
        {
            Id = plan.Id,
            Code = plan.Code,
            DisplayName = plan.DisplayName,
            Description = plan.Description,
            PlanType = plan.PlanType,
            DurationMonths = plan.DurationMonths,
            Price = plan.Price,
            Currency = plan.Currency,
            IsActive = plan.IsActive,
            SortOrder = plan.SortOrder,
            IsSystem = plan.IsSystem,
            CreationTime = plan.CreationTime,
            CreatorId = plan.CreatorId,
            LastModificationTime = plan.LastModificationTime,
            LastModifierId = plan.LastModifierId,
            IsDeleted = plan.IsDeleted,
            DeleterId = plan.DeleterId,
            DeletionTime = plan.DeletionTime
        };
    }
}
