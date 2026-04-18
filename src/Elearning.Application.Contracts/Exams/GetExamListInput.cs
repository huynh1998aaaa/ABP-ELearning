using Volo.Abp.Application.Dtos;

namespace Elearning.Exams;

public class GetExamListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }

    public ExamStatus? Status { get; set; }

    public ExamAccessLevel? AccessLevel { get; set; }

    public ExamSelectionMode? SelectionMode { get; set; }

    public bool? IsActive { get; set; }
}
