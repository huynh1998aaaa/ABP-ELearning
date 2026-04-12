using System.ComponentModel.DataAnnotations;

namespace Elearning.PremiumSubscriptions;

public class UpdatePremiumPlanDto
{
    [Required]
    [StringLength(PremiumPlanConsts.MaxCodeLength)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(PremiumPlanConsts.MaxDisplayNameLength)]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(PremiumPlanConsts.MaxDescriptionLength)]
    public string? Description { get; set; }

    public PremiumPlanType PlanType { get; set; } = PremiumPlanType.SixMonths;

    [Range(1, 120)]
    public int DurationMonths { get; set; } = PremiumPlanConsts.SixMonthsDuration;

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Required]
    [StringLength(PremiumPlanConsts.MaxCurrencyLength)]
    public string Currency { get; set; } = "VND";

    public int SortOrder { get; set; }
}
