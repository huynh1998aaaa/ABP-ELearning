using Volo.Abp.Application.Dtos;

namespace Elearning.Exams;

public class GetExamAvailableQuestionListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
}
