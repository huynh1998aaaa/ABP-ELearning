using System.ComponentModel.DataAnnotations;

namespace Elearning.Practices;

public class UpdatePracticeSetDto
{
    [Required]
    [StringLength(PracticeSetConsts.MaxCodeLength)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(PracticeSetConsts.MaxTitleLength)]
    public string Title { get; set; } = string.Empty;

    [StringLength(PracticeSetConsts.MaxDescriptionLength)]
    public string? Description { get; set; }

    public PracticeAccessLevel AccessLevel { get; set; }

    public PracticeSelectionMode SelectionMode { get; set; }

    [Range(PracticeSetConsts.MinQuestionCount, PracticeSetConsts.MaxQuestionCount)]
    public int TotalQuestionCount { get; set; }

    public bool ShuffleQuestions { get; set; }

    public bool ShowExplanation { get; set; }

    public int SortOrder { get; set; }
}
