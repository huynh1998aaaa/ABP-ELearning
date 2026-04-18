using Volo.Abp.Application.Dtos;

namespace Elearning.Practices;

public class GetPracticeSetListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }

    public PracticeStatus? Status { get; set; }

    public PracticeAccessLevel? AccessLevel { get; set; }

    public PracticeSelectionMode? SelectionMode { get; set; }

    public bool? IsActive { get; set; }
}
