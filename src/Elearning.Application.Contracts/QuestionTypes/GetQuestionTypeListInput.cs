using Volo.Abp.Application.Dtos;

namespace Elearning.QuestionTypes;

public class GetQuestionTypeListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }

    public bool? IsActive { get; set; }
}
