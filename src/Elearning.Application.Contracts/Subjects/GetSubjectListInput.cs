using Volo.Abp.Application.Dtos;

namespace Elearning.Subjects;

public class GetSubjectListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }

    public bool? IsActive { get; set; }
}
