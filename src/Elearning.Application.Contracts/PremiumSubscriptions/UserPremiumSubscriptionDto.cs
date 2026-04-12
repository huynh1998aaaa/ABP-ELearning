using System;
using Volo.Abp.Application.Dtos;

namespace Elearning.PremiumSubscriptions;

public class UserPremiumSubscriptionDto : FullAuditedEntityDto<Guid>
{
    public Guid UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public Guid PremiumPlanId { get; set; }

    public string PremiumPlanCode { get; set; } = string.Empty;

    public string PremiumPlanName { get; set; } = string.Empty;

    public int ActivationNumber { get; set; }

    public DateTime ActivatedTime { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public PremiumSubscriptionStatus Status { get; set; }

    public bool IsCurrentlyActive { get; set; }

    public int RemainingDays { get; set; }

    public string? Note { get; set; }

    public DateTime? CancelledTime { get; set; }

    public string? CancellationReason { get; set; }
}
