using System;
using Volo.Abp.Application.Dtos;

namespace Elearning.PremiumSubscriptions;

public class PremiumPlanDto : FullAuditedEntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public PremiumPlanType PlanType { get; set; }

    public int DurationMonths { get; set; }

    public decimal Price { get; set; }

    public string Currency { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public int SortOrder { get; set; }

    public bool IsSystem { get; set; }
}
