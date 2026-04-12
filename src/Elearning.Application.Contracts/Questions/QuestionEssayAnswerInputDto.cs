using System.ComponentModel.DataAnnotations;

namespace Elearning.Questions;

public class QuestionEssayAnswerInputDto
{
    [StringLength(QuestionConsts.MaxSampleAnswerLength)]
    public string? SampleAnswer { get; set; }

    [StringLength(QuestionConsts.MaxRubricLength)]
    public string? Rubric { get; set; }

    [Range(0, int.MaxValue)]
    public int? MaxWords { get; set; }
}
