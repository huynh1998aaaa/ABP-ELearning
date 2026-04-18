using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Elearning.LearningSessions;

public class LearningSession : FullAuditedAggregateRoot<Guid>
{
    public Guid UserId { get; private set; }

    public LearningSessionSourceKind SourceKind { get; private set; }

    public Guid SourceId { get; private set; }

    public string SourceCode { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsPremiumContent { get; private set; }

    public bool ShowExplanation { get; private set; }

    public int? DurationMinutes { get; private set; }

    public DateTime StartedAt { get; private set; }

    public DateTime? EndsAt { get; private set; }

    public DateTime? SubmittedAt { get; private set; }

    public LearningSessionStatus Status { get; private set; }

    public decimal Score { get; private set; }

    public int CorrectCount { get; private set; }

    public int AnsweredCount { get; private set; }

    public int TotalQuestionCount { get; private set; }

    protected LearningSession()
    {
    }

    public LearningSession(
        Guid id,
        Guid userId,
        LearningSessionSourceKind sourceKind,
        Guid sourceId,
        string sourceCode,
        string title,
        string? description,
        bool isPremiumContent,
        bool showExplanation,
        int? durationMinutes,
        int totalQuestionCount,
        DateTime startedAt)
        : base(id)
    {
        UserId = userId;
        SourceKind = sourceKind;
        SourceId = sourceId;
        SourceCode = Check.NotNullOrWhiteSpace(sourceCode, nameof(sourceCode), LearningSessionConsts.MaxSourceCodeLength);
        Title = Check.NotNullOrWhiteSpace(title, nameof(title), LearningSessionConsts.MaxTitleLength);
        Description = Check.Length(description, nameof(description), LearningSessionConsts.MaxDescriptionLength);
        IsPremiumContent = isPremiumContent;
        ShowExplanation = showExplanation;
        DurationMinutes = durationMinutes;
        TotalQuestionCount = Check.Range(totalQuestionCount, nameof(totalQuestionCount), 1, int.MaxValue);
        StartedAt = startedAt;
        EndsAt = durationMinutes.HasValue ? startedAt.AddMinutes(durationMinutes.Value) : null;
        Status = LearningSessionStatus.InProgress;
    }

    public bool HasTimeLimit => EndsAt.HasValue;

    public void Submit(DateTime submittedAt, decimal score, int correctCount, int answeredCount)
    {
        if (Status != LearningSessionStatus.InProgress)
        {
            return;
        }

        SubmittedAt = submittedAt;
        Status = LearningSessionStatus.Submitted;
        Score = Check.Range(score, nameof(score), 0, decimal.MaxValue);
        CorrectCount = Check.Range(correctCount, nameof(correctCount), 0, int.MaxValue);
        AnsweredCount = Check.Range(answeredCount, nameof(answeredCount), 0, int.MaxValue);
    }

    public void Abandon()
    {
        if (Status == LearningSessionStatus.InProgress)
        {
            Status = LearningSessionStatus.Abandoned;
        }
    }
}
