using System;
using System.ComponentModel.DataAnnotations;

namespace Elearning.PremiumSubscriptions;

public class CreateUserPremiumSubscriptionDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid PremiumPlanId { get; set; }

    [StringLength(PremiumSubscriptionConsts.MaxNoteLength)]
    public string? Note { get; set; }
}
