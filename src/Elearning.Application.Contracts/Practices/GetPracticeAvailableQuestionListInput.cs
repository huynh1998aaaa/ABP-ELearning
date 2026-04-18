using Volo.Abp.Application.Dtos;

namespace Elearning.Practices;

public class GetPracticeAvailableQuestionListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
}
