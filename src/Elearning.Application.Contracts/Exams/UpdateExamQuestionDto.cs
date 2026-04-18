using System.ComponentModel.DataAnnotations;

namespace Elearning.Exams;

public class UpdateExamQuestionDto
{
    public int SortOrder { get; set; }

    [Range(0, 999999)]
    public decimal? ScoreOverride { get; set; }

    public bool IsRequired { get; set; }
}
