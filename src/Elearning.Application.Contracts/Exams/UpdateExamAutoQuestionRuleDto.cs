using System;
using System.ComponentModel.DataAnnotations;
using Elearning.Questions;

namespace Elearning.Exams;

public class UpdateExamAutoQuestionRuleDto
{
    [Required]
    public Guid QuestionTypeId { get; set; }

    public QuestionDifficulty? Difficulty { get; set; }

    [Range(1, ExamConsts.MaxQuestionCount)]
    public int TargetCount { get; set; }

    public int SortOrder { get; set; }
}
