using System;
using System.ComponentModel.DataAnnotations;

namespace Elearning.Exams;

public class AddExamQuestionDto
{
    [Required]
    public Guid QuestionId { get; set; }

    public int SortOrder { get; set; }

    [Range(0, 999999)]
    public decimal? ScoreOverride { get; set; }

    public bool IsRequired { get; set; }
}
