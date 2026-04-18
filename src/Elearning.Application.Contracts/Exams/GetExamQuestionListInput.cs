using Volo.Abp.Application.Dtos;

namespace Elearning.Exams;

public class GetExamQuestionListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
}
