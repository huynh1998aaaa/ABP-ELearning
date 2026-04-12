using Volo.Abp.Application.Dtos;

namespace Elearning.PremiumSubscriptions;

public class GetPremiumPlanListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }

    public bool? IsActive { get; set; }
}
