using System.ComponentModel.DataAnnotations;

namespace Elearning.Exams;

public class CreateExamDto
{
    [Required]
    [StringLength(ExamConsts.MaxCodeLength)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(ExamConsts.MaxTitleLength)]
    public string Title { get; set; } = string.Empty;

    [StringLength(ExamConsts.MaxDescriptionLength)]
    public string? Description { get; set; }

    public ExamAccessLevel AccessLevel { get; set; } = ExamAccessLevel.Free;

    public ExamSelectionMode SelectionMode { get; set; } = ExamSelectionMode.Fixed;

    [Range(ExamConsts.MinDurationMinutes, ExamConsts.MaxDurationMinutes)]
    public int DurationMinutes { get; set; } = 60;

    [Range(ExamConsts.MinQuestionCount, ExamConsts.MaxQuestionCount)]
    public int TotalQuestionCount { get; set; } = 1;

    [Range(0, 999999)]
    public decimal? PassingScore { get; set; }

    public bool ShuffleQuestions { get; set; }

    public bool ShuffleOptions { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; } = 100;
}
