using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Elearning.PremiumSubscriptions;

public class PremiumPlan : FullAuditedAggregateRoot<Guid>
{
    public string Code { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public PremiumPlanType PlanType { get; private set; }

    public int DurationMonths { get; private set; }

    public decimal Price { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsSystem { get; private set; }

    protected PremiumPlan()
    {
    }

    public PremiumPlan(
        Guid id,
        string code,
        string displayName,
        PremiumPlanType planType,
        int durationMonths,
        decimal price,
        string currency,
        int sortOrder,
        bool isSystem = false,
        string? description = null)
        : base(id)
    {
        SetCode(code);
        UpdateDetails(displayName, description, planType, durationMonths, price, currency, sortOrder);
        IsSystem = isSystem;
        IsActive = true;
    }

    public void UpdateDetails(
        string displayName,
        string? description,
        PremiumPlanType planType,
        int durationMonths,
        decimal price,
        string currency,
        int sortOrder)
    {
        DisplayName = Check.NotNullOrWhiteSpace(displayName, nameof(displayName), PremiumPlanConsts.MaxDisplayNameLength);
        Description = Check.Length(description, nameof(description), PremiumPlanConsts.MaxDescriptionLength);
        PlanType = planType;
        DurationMonths = Check.Range(durationMonths, nameof(durationMonths), 1, 120);
        Price = Check.Range(price, nameof(price), 0, decimal.MaxValue);
        Currency = Check.NotNullOrWhiteSpace(currency, nameof(currency), PremiumPlanConsts.MaxCurrencyLength);
        SortOrder = sortOrder;
    }

    public void SetCode(string code)
    {
        Code = Check.NotNullOrWhiteSpace(code, nameof(code), PremiumPlanConsts.MaxCodeLength);
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
