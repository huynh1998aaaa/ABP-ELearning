using System;
using Elearning.Questions;

namespace Elearning.Practices;

public class PracticeAvailableQuestionDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string QuestionTypeName { get; set; } = string.Empty;

    public QuestionDifficulty Difficulty { get; set; }

    public QuestionStatus Status { get; set; }

    public bool IsActive { get; set; }

    public decimal Score { get; set; }
}
