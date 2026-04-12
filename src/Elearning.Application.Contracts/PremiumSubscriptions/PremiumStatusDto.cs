using System;

namespace Elearning.PremiumSubscriptions;

public class PremiumStatusDto
{
    public bool IsPremium { get; set; }

    public Guid? SubscriptionId { get; set; }

    public Guid? PremiumPlanId { get; set; }

    public string PremiumPlanName { get; set; } = string.Empty;

    public int? ActivationNumber { get; set; }

    public DateTime? ActivatedTime { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public PremiumSubscriptionStatus? Status { get; set; }

    public int RemainingDays { get; set; }
}
