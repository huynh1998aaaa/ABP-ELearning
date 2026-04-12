using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Elearning.Questions;

public class QuestionOption : FullAuditedEntity<Guid>
{
    public Guid QuestionId { get; private set; }

    public string Text { get; private set; } = string.Empty;

    public bool IsCorrect { get; private set; }

    public int SortOrder { get; private set; }

    protected QuestionOption()
    {
    }

    public QuestionOption(Guid id, Guid questionId, string text, bool isCorrect, int sortOrder)
        : base(id)
    {
        QuestionId = questionId;
        Update(text, isCorrect, sortOrder);
    }

    public void Update(string text, bool isCorrect, int sortOrder)
    {
        Text = Check.NotNullOrWhiteSpace(text, nameof(text), QuestionConsts.MaxOptionTextLength);
        IsCorrect = isCorrect;
        SortOrder = sortOrder;
    }
}
