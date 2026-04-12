using System;
using Volo.Abp.Application.Dtos;

namespace Elearning.PremiumSubscriptions;

public class GetUserPremiumSubscriptionListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }

    public PremiumSubscriptionStatus? Status { get; set; }

    public Guid? PremiumPlanId { get; set; }
}
