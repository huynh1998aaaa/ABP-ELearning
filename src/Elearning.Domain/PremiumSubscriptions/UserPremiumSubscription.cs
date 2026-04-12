using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Elearning.PremiumSubscriptions;

public class UserPremiumSubscription : FullAuditedAggregateRoot<Guid>
{
    public Guid UserId { get; private set; }

    public Guid PremiumPlanId { get; private set; }

    public int ActivationNumber { get; private set; }

    public DateTime ActivatedTime { get; private set; }

    public DateTime StartTime { get; private set; }

    public DateTime EndTime { get; private set; }

    public PremiumSubscriptionStatus Status { get; private set; }

    public string? Note { get; private set; }

    public DateTime? CancelledTime { get; private set; }

    public string? CancellationReason { get; private set; }

    protected UserPremiumSubscription()
    {
    }

    public UserPremiumSubscription(
        Guid id,
        Guid userId,
        Guid premiumPlanId,
        int activationNumber,
        DateTime activatedTime,
        int durationMonths,
        string? note = null)
        : base(id)
    {
        UserId = userId;
        PremiumPlanId = premiumPlanId;
        ActivationNumber = Check.Range(activationNumber, nameof(activationNumber), 1, int.MaxValue);
        ActivatedTime = activatedTime;
        StartTime = activatedTime;
        EndTime = StartTime.AddMonths(Check.Range(durationMonths, nameof(durationMonths), 1, 120));
        Status = PremiumSubscriptionStatus.Active;
        Note = Check.Length(note, nameof(note), PremiumSubscriptionConsts.MaxNoteLength);
    }

    public bool IsCurrentlyActive(DateTime now)
    {
        return Status == PremiumSubscriptionStatus.Active && StartTime <= now && EndTime > now;
    }

    public void Extend(int durationMonths)
    {
        EnsureNotCancelled();
        EndTime = EndTime.AddMonths(Check.Range(durationMonths, nameof(durationMonths), 1, 120));
        Status = PremiumSubscriptionStatus.Active;
    }

    public void Cancel(DateTime cancelledTime, string? cancellationReason)
    {
        EnsureNotCancelled();
        Status = PremiumSubscriptionStatus.Cancelled;
        CancelledTime = cancelledTime;
        CancellationReason = Check.Length(cancellationReason, nameof(cancellationReason), PremiumSubscriptionConsts.MaxCancellationReasonLength);
    }

    public void MarkExpired()
    {
        EnsureNotCancelled();
        Status = PremiumSubscriptionStatus.Expired;
    }

    private void EnsureNotCancelled()
    {
        if (Status == PremiumSubscriptionStatus.Cancelled)
        {
            throw new BusinessException(ElearningDomainErrorCodes.CancelledPremiumSubscriptionCannotBeChanged);
        }
    }
}
