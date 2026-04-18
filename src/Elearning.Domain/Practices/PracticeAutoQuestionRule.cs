using System;
using Elearning.Questions;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Elearning.Practices;

public class PracticeAutoQuestionRule : FullAuditedEntity<Guid>
{
    public Guid PracticeSetId { get; private set; }

    public Guid QuestionTypeId { get; private set; }

    public QuestionDifficulty? Difficulty { get; private set; }

    public int TargetCount { get; private set; }

    public int SortOrder { get; private set; }

    protected PracticeAutoQuestionRule()
    {
    }

    public PracticeAutoQuestionRule(
        Guid id,
        Guid practiceSetId,
        Guid questionTypeId,
        int targetCount,
        int sortOrder,
        QuestionDifficulty? difficulty = null)
        : base(id)
    {
        PracticeSetId = practiceSetId;
        UpdateDetails(questionTypeId, targetCount, sortOrder, difficulty);
    }

    public void UpdateDetails(
        Guid questionTypeId,
        int targetCount,
        int sortOrder,
        QuestionDifficulty? difficulty = null)
    {
        QuestionTypeId = Check.NotNull(questionTypeId, nameof(questionTypeId));
        TargetCount = Check.Range(targetCount, nameof(targetCount), 1, PracticeSetConsts.MaxQuestionCount);
        SortOrder = sortOrder;
        Difficulty = difficulty;
    }
}
