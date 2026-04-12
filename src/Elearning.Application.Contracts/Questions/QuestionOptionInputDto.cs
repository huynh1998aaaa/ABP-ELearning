using System.ComponentModel.DataAnnotations;

namespace Elearning.Questions;

public class QuestionOptionInputDto
{
    [StringLength(QuestionConsts.MaxOptionTextLength)]
    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }

    public int SortOrder { get; set; }
}
