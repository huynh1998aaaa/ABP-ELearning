using System.ComponentModel.DataAnnotations;

namespace Elearning.PremiumSubscriptions;

public class CancelPremiumSubscriptionDto
{
    [StringLength(PremiumSubscriptionConsts.MaxCancellationReasonLength)]
    public string? Reason { get; set; }
}
