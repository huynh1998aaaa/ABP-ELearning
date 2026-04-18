using System.ComponentModel.DataAnnotations;

namespace Elearning.Exams;

public class UpdateExamDto
{
    [Required]
    [StringLength(ExamConsts.MaxCodeLength)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(ExamConsts.MaxTitleLength)]
    public string Title { get; set; } = string.Empty;

    [StringLength(ExamConsts.MaxDescriptionLength)]
    public string? Description { get; set; }

    public ExamAccessLevel AccessLevel { get; set; }

    public ExamSelectionMode SelectionMode { get; set; }

    [Range(ExamConsts.MinDurationMinutes, ExamConsts.MaxDurationMinutes)]
    public int DurationMinutes { get; set; }

    [Range(ExamConsts.MinQuestionCount, ExamConsts.MaxQuestionCount)]
    public int TotalQuestionCount { get; set; }

    [Range(0, 999999)]
    public decimal? PassingScore { get; set; }

    public bool ShuffleQuestions { get; set; }

    public bool ShuffleOptions { get; set; }

    public int SortOrder { get; set; }
}
