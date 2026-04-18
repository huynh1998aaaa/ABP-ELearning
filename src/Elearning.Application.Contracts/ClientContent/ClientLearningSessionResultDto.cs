using System;
using System.Collections.Generic;
using Elearning.LearningSessions;

namespace Elearning.ClientContent;

public class ClientLearningSessionResultDto
{
    public Guid Id { get; set; }

    public LearningSessionSourceKind SourceKind { get; set; }

    public string Title { get; set; } = string.Empty;

    public bool ShowExplanation { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public decimal Score { get; set; }

    public int CorrectCount { get; set; }

    public int AnsweredCount { get; set; }

    public int TotalQuestionCount { get; set; }

    public int PendingManualGradingCount { get; set; }

    public List<ClientLearningSessionQuestionDto> Questions { get; set; } = new();
}
