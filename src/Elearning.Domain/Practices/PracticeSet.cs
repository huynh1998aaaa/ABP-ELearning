using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Elearning.Practices;

public class PracticeSet : FullAuditedAggregateRoot<Guid>
{
    public string Code { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public PracticeStatus Status { get; private set; }

    public PracticeAccessLevel AccessLevel { get; private set; }

    public PracticeSelectionMode SelectionMode { get; private set; }

    public int TotalQuestionCount { get; private set; }

    public bool ShuffleQuestions { get; private set; }

    public bool ShowExplanation { get; private set; }

    public bool IsActive { get; private set; }

    public int SortOrder { get; private set; }

    public DateTime? PublishedTime { get; private set; }

    public DateTime? ArchivedTime { get; private set; }

    protected PracticeSet()
    {
    }

    public PracticeSet(
        Guid id,
        string code,
        string title,
        PracticeAccessLevel accessLevel,
        PracticeSelectionMode selectionMode,
        int totalQuestionCount,
        int sortOrder,
        string? description = null,
        bool shuffleQuestions = false,
        bool showExplanation = true)
        : base(id)
    {
        Status = PracticeStatus.Draft;
        IsActive = true;
        UpdateDetails(
            code,
            title,
            description,
            accessLevel,
            selectionMode,
            totalQuestionCount,
            shuffleQuestions,
            showExplanation,
            sortOrder);
    }

    public void UpdateDetails(
        string code,
        string title,
        string? description,
        PracticeAccessLevel accessLevel,
        PracticeSelectionMode selectionMode,
        int totalQuestionCount,
        bool shuffleQuestions,
        bool showExplanation,
        int sortOrder)
    {
        Code = Check.NotNullOrWhiteSpace(code, nameof(code), PracticeSetConsts.MaxCodeLength);
        Title = Check.NotNullOrWhiteSpace(title, nameof(title), PracticeSetConsts.MaxTitleLength);
        Description = Check.Length(description, nameof(description), PracticeSetConsts.MaxDescriptionLength);
        AccessLevel = accessLevel;
        SelectionMode = selectionMode;
        TotalQuestionCount = Check.Range(totalQuestionCount, nameof(totalQuestionCount), PracticeSetConsts.MinQuestionCount, PracticeSetConsts.MaxQuestionCount);
        ShuffleQuestions = shuffleQuestions;
        ShowExplanation = showExplanation;
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
        Status = PracticeStatus.Published;
        PublishedTime = publishedTime;
        ArchivedTime = null;
    }

    public void Archive(DateTime archivedTime)
    {
        Status = PracticeStatus.Archived;
        ArchivedTime = archivedTime;
        IsActive = false;
    }
}
