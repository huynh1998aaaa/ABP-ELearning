using System;
using System.ComponentModel.DataAnnotations;
using Elearning.Questions;

namespace Elearning.Exams;

public class CreateExamAutoQuestionRuleDto
{
    [Required]
    public Guid QuestionTypeId { get; set; }

    public QuestionDifficulty? Difficulty { get; set; }

    [Range(1, ExamConsts.MaxQuestionCount)]
    public int TargetCount { get; set; } = 1;

    public int SortOrder { get; set; } = 10;
}
