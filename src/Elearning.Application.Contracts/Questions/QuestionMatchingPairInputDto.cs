using System.ComponentModel.DataAnnotations;

namespace Elearning.Questions;

public class QuestionMatchingPairInputDto
{
    [StringLength(QuestionConsts.MaxMatchingTextLength)]
    public string LeftText { get; set; } = string.Empty;

    [StringLength(QuestionConsts.MaxMatchingTextLength)]
    public string RightText { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
