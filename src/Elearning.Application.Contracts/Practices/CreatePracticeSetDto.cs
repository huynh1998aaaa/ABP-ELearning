using System.ComponentModel.DataAnnotations;

namespace Elearning.Practices;

public class CreatePracticeSetDto
{
    [Required]
    [StringLength(PracticeSetConsts.MaxCodeLength)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(PracticeSetConsts.MaxTitleLength)]
    public string Title { get; set; } = string.Empty;

    [StringLength(PracticeSetConsts.MaxDescriptionLength)]
    public string? Description { get; set; }

    public PracticeAccessLevel AccessLevel { get; set; } = PracticeAccessLevel.Free;

    public PracticeSelectionMode SelectionMode { get; set; } = PracticeSelectionMode.Fixed;

    [Range(PracticeSetConsts.MinQuestionCount, PracticeSetConsts.MaxQuestionCount)]
    public int TotalQuestionCount { get; set; } = 1;

    public bool ShuffleQuestions { get; set; }

    public bool ShowExplanation { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; } = 100;
}
