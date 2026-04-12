using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Elearning.Questions;

public class QuestionMatchingPair : FullAuditedEntity<Guid>
{
    public Guid QuestionId { get; private set; }

    public string LeftText { get; private set; } = string.Empty;

    public string RightText { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    protected QuestionMatchingPair()
    {
    }

    public QuestionMatchingPair(Guid id, Guid questionId, string leftText, string rightText, int sortOrder)
        : base(id)
    {
        QuestionId = questionId;
        Update(leftText, rightText, sortOrder);
    }

    public void Update(string leftText, string rightText, int sortOrder)
    {
        LeftText = Check.NotNullOrWhiteSpace(leftText, nameof(leftText), QuestionConsts.MaxMatchingTextLength);
        RightText = Check.NotNullOrWhiteSpace(rightText, nameof(rightText), QuestionConsts.MaxMatchingTextLength);
        SortOrder = sortOrder;
    }
}
