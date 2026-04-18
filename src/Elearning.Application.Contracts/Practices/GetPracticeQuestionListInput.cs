using Volo.Abp.Application.Dtos;

namespace Elearning.Practices;

public class GetPracticeQuestionListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
}
