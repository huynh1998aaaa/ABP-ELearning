using System.ComponentModel.DataAnnotations;

namespace Elearning.QuestionTypes;

public class CreateQuestionTypeDto
{
    [Required]
    [StringLength(QuestionTypeConsts.MaxCodeLength)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(QuestionTypeConsts.MaxDisplayNameLength)]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(QuestionTypeConsts.MaxDescriptionLength)]
    public string? Description { get; set; }

    public QuestionInputKind InputKind { get; set; } = QuestionInputKind.SingleChoice;

    public QuestionScoringKind ScoringKind { get; set; } = QuestionScoringKind.Auto;

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public bool SupportsOptions { get; set; }

    public bool SupportsAnswerPairs { get; set; }

    public bool RequiresManualGrading { get; set; }

    public bool AllowMultipleCorrectAnswers { get; set; }

    [Range(0, int.MaxValue)]
    public int? MinimumOptions { get; set; }

    [Range(0, int.MaxValue)]
    public int? MaximumOptions { get; set; }
}
