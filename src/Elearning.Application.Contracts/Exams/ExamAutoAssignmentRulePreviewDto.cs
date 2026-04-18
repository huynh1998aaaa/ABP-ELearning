using System;
using Elearning.Questions;

namespace Elearning.Exams;

public class ExamAutoAssignmentRulePreviewDto
{
    public Guid RuleId { get; set; }

    public Guid QuestionTypeId { get; set; }

    public string QuestionTypeName { get; set; } = string.Empty;

    public QuestionDifficulty? Difficulty { get; set; }

    public int RequestedCount { get; set; }

    public int FulfilledByManualCount { get; set; }

    public int AutoAssignableCount { get; set; }

    public int ShortageCount { get; set; }
}
