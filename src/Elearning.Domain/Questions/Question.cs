using System;
using Elearning.QuestionTypes;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Elearning.Questions;

public class Question : FullAuditedAggregateRoot<Guid>
{
    public Guid QuestionTypeId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public string? Explanation { get; private set; }

    public QuestionDifficulty Difficulty { get; private set; }

    public decimal Score { get; private set; }

    public bool IsActive { get; private set; }

    public int SortOrder { get; private set; }

    public QuestionStatus Status { get; private set; }

    protected Question()
    {
    }

    public Question(
        Guid id,
        Guid questionTypeId,
        string title,
        string content,
        QuestionDifficulty difficulty,
        decimal score,
        int sortOrder,
        string? explanation = null)
        : base(id)
    {
        QuestionTypeId = questionTypeId;
        Status = QuestionStatus.Draft;
        IsActive = true;
        UpdateDetails(title, content, explanation, difficulty, score, sortOrder);
    }

    public void UpdateDetails(
        string title,
        string content,
        string? explanation,
        QuestionDifficulty difficulty,
        decimal score,
        int sortOrder)
    {
        Title = Check.NotNullOrWhiteSpace(title, nameof(title), QuestionConsts.MaxTitleLength);
        Content = Check.NotNullOrWhiteSpace(content, nameof(content), QuestionConsts.MaxContentLength);
        Explanation = Check.Length(explanation, nameof(explanation), QuestionConsts.MaxExplanationLength);
        Difficulty = difficulty;
        Score = Check.Range(score, nameof(score), 0, decimal.MaxValue);
        SortOrder = sortOrder;
    }

    public void EnsureQuestionType(Guid questionTypeId)
    {
        if (QuestionTypeId != questionTypeId)
        {
            throw new BusinessException(ElearningDomainErrorCodes.QuestionTypeCannotBeChanged)
                .WithData(nameof(QuestionTypeId), QuestionTypeId);
        }
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Publish()
    {
        Status = QuestionStatus.Published;
    }

    public void Archive()
    {
        Status = QuestionStatus.Archived;
        IsActive = false;
    }
}
