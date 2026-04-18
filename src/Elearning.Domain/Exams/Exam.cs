using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Elearning.Exams;

public class Exam : FullAuditedAggregateRoot<Guid>
{
    public string Code { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public ExamStatus Status { get; private set; }

    public ExamAccessLevel AccessLevel { get; private set; }

    public ExamSelectionMode SelectionMode { get; private set; }

    public int DurationMinutes { get; private set; }

    public int TotalQuestionCount { get; private set; }

    public decimal? PassingScore { get; private set; }

    public bool ShuffleQuestions { get; private set; }

    public bool ShuffleOptions { get; private set; }

    public bool IsActive { get; private set; }

    public int SortOrder { get; private set; }

    public DateTime? PublishedTime { get; private set; }

    public DateTime? ArchivedTime { get; private set; }

    protected Exam()
    {
    }

    public Exam(
        Guid id,
        string code,
        string title,
        ExamAccessLevel accessLevel,
        ExamSelectionMode selectionMode,
        int durationMinutes,
        int totalQuestionCount,
        int sortOrder,
        string? description = null,
        decimal? passingScore = null,
        bool shuffleQuestions = false,
        bool shuffleOptions = false)
        : base(id)
    {
        Status = ExamStatus.Draft;
        IsActive = true;
        UpdateDetails(
            code,
            title,
            description,
            accessLevel,
            selectionMode,
            durationMinutes,
            totalQuestionCount,
            passingScore,
            shuffleQuestions,
            shuffleOptions,
            sortOrder);
    }

    public void UpdateDetails(
        string code,
        string title,
        string? description,
        ExamAccessLevel accessLevel,
        ExamSelectionMode selectionMode,
        int durationMinutes,
        int totalQuestionCount,
        decimal? passingScore,
        bool shuffleQuestions,
        bool shuffleOptions,
        int sortOrder)
    {
        Code = Check.NotNullOrWhiteSpace(code, nameof(code), ExamConsts.MaxCodeLength);
        Title = Check.NotNullOrWhiteSpace(title, nameof(title), ExamConsts.MaxTitleLength);
        Description = Check.Length(description, nameof(description), ExamConsts.MaxDescriptionLength);
        AccessLevel = accessLevel;
        SelectionMode = selectionMode;
        DurationMinutes = Check.Range(durationMinutes, nameof(durationMinutes), ExamConsts.MinDurationMinutes, ExamConsts.MaxDurationMinutes);
        TotalQuestionCount = Check.Range(totalQuestionCount, nameof(totalQuestionCount), ExamConsts.MinQuestionCount, ExamConsts.MaxQuestionCount);
        PassingScore = passingScore.HasValue ? Check.Range(passingScore.Value, nameof(passingScore), 0, decimal.MaxValue) : null;
        ShuffleQuestions = shuffleQuestions;
        ShuffleOptions = shuffleOptions;
        SortOrder = sortOrder;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Publish(DateTime publishedTime)
    {
        Status = ExamStatus.Published;
        PublishedTime = publishedTime;
        ArchivedTime = null;
    }

    public void Archive(DateTime archivedTime)
    {
        Status = ExamStatus.Archived;
        ArchivedTime = archivedTime;
        IsActive = false;
    }
}
