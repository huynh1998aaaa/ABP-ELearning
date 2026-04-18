using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Elearning.LearningSessions;

public class LearningSessionQuestionMatchingPair : FullAuditedEntity<Guid>
{
    public Guid LearningSessionQuestionId { get; private set; }

    public Guid OriginalQuestionMatchingPairId { get; private set; }

    public string LeftText { get; private set; } = string.Empty;

    public string RightText { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    protected LearningSessionQuestionMatchingPair()
    {
    }

    public LearningSessionQuestionMatchingPair(
        Guid id,
        Guid learningSessionQuestionId,
        Guid originalQuestionMatchingPairId,
        string leftText,
        string rightText,
        int sortOrder)
        : base(id)
    {
        LearningSessionQuestionId = learningSessionQuestionId;
        OriginalQuestionMatchingPairId = originalQuestionMatchingPairId;
        LeftText = Check.NotNullOrWhiteSpace(leftText, nameof(leftText), Questions.QuestionConsts.MaxMatchingTextLength);
        RightText = Check.NotNullOrWhiteSpace(rightText, nameof(rightText), Questions.QuestionConsts.MaxMatchingTextLength);
        SortOrder = sortOrder;
    }
}
