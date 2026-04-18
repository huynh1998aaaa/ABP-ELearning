using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Elearning.LearningSessions;

public class LearningSessionQuestionOption : FullAuditedEntity<Guid>
{
    public Guid LearningSessionQuestionId { get; private set; }

    public Guid OriginalQuestionOptionId { get; private set; }

    public string Text { get; private set; } = string.Empty;

    public bool IsCorrect { get; private set; }

    public int SortOrder { get; private set; }

    protected LearningSessionQuestionOption()
    {
    }

    public LearningSessionQuestionOption(
        Guid id,
        Guid learningSessionQuestionId,
        Guid originalQuestionOptionId,
        string text,
        bool isCorrect,
        int sortOrder)
        : base(id)
    {
        LearningSessionQuestionId = learningSessionQuestionId;
        OriginalQuestionOptionId = originalQuestionOptionId;
        Text = Check.NotNullOrWhiteSpace(text, nameof(text), Questions.QuestionConsts.MaxOptionTextLength);
        IsCorrect = isCorrect;
        SortOrder = sortOrder;
    }
}
