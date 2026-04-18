using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Elearning.LearningSessions;

public class LearningSessionQuestion : FullAuditedEntity<Guid>
{
    public Guid LearningSessionId { get; private set; }

    public Guid OriginalQuestionId { get; private set; }

    public Guid QuestionTypeId { get; private set; }

    public string QuestionTypeCode { get; private set; } = string.Empty;

    public string QuestionTypeName { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public string? Explanation { get; private set; }

    public int SortOrder { get; private set; }

    public decimal Score { get; private set; }

    protected LearningSessionQuestion()
    {
    }

    public LearningSessionQuestion(
        Guid id,
        Guid learningSessionId,
        Guid originalQuestionId,
        Guid questionTypeId,
        string questionTypeCode,
        string questionTypeName,
        string title,
        string content,
        string? explanation,
        int sortOrder,
        decimal score)
        : base(id)
    {
        LearningSessionId = learningSessionId;
        OriginalQuestionId = originalQuestionId;
        QuestionTypeId = questionTypeId;
        QuestionTypeCode = Check.NotNullOrWhiteSpace(questionTypeCode, nameof(questionTypeCode), LearningSessionConsts.MaxQuestionTypeCodeLength);
        QuestionTypeName = Check.NotNullOrWhiteSpace(questionTypeName, nameof(questionTypeName), LearningSessionConsts.MaxQuestionTypeNameLength);
        Title = Check.NotNullOrWhiteSpace(title, nameof(title), LearningSessionConsts.MaxTitleLength);
        Content = Check.NotNullOrWhiteSpace(content, nameof(content), Questions.QuestionConsts.MaxContentLength);
        Explanation = Check.Length(explanation, nameof(explanation), Questions.QuestionConsts.MaxExplanationLength);
        SortOrder = sortOrder;
        Score = Check.Range(score, nameof(score), 0, decimal.MaxValue);
    }
}
