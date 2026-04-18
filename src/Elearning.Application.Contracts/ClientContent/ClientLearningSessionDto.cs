using System;
using System.Collections.Generic;
using Elearning.LearningSessions;

namespace Elearning.ClientContent;

public class ClientLearningSessionDto
{
    public Guid Id { get; set; }

    public LearningSessionSourceKind SourceKind { get; set; }

    public Guid SourceId { get; set; }

    public string SourceCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsPremiumContent { get; set; }

    public bool ShowExplanation { get; set; }

    public int? DurationMinutes { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? EndsAt { get; set; }

    public LearningSessionStatus Status { get; set; }

    public int AnsweredCount { get; set; }

    public int TotalQuestionCount { get; set; }

    public decimal Score { get; set; }

    public int CorrectCount { get; set; }

    public List<ClientLearningSessionQuestionDto> Questions { get; set; } = new();
}
