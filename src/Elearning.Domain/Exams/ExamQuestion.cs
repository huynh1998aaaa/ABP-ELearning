using System;
using Elearning.Questions;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Elearning.Exams;

public class ExamQuestion : FullAuditedEntity<Guid>
{
    public Guid ExamId { get; private set; }

    public Guid QuestionId { get; private set; }

    public int SortOrder { get; private set; }

    public decimal? ScoreOverride { get; private set; }

    public bool IsRequired { get; private set; }

    public QuestionAssignmentSource AssignmentSource { get; private set; }

    protected ExamQuestion()
    {
    }

    public ExamQuestion(
        Guid id,
        Guid examId,
        Guid questionId,
        int sortOrder,
        decimal? scoreOverride = null,
        bool isRequired = false,
        QuestionAssignmentSource assignmentSource = QuestionAssignmentSource.Manual)
        : base(id)
    {
        ExamId = examId;
        QuestionId = questionId;
        AssignmentSource = assignmentSource;
        UpdateDetails(sortOrder, scoreOverride, isRequired);
    }

    public void UpdateDetails(int sortOrder, decimal? scoreOverride, bool isRequired)
    {
        SortOrder = sortOrder;
        ScoreOverride = scoreOverride.HasValue ? Check.Range(scoreOverride.Value, nameof(scoreOverride), 0, decimal.MaxValue) : null;
        IsRequired = isRequired;
    }
}
