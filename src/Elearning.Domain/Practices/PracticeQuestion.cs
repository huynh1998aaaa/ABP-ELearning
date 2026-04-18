using System;
using Elearning.Questions;
using Volo.Abp.Domain.Entities.Auditing;

namespace Elearning.Practices;

public class PracticeQuestion : FullAuditedEntity<Guid>
{
    public Guid PracticeSetId { get; private set; }

    public Guid QuestionId { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsRequired { get; private set; }

    public QuestionAssignmentSource AssignmentSource { get; private set; }

    protected PracticeQuestion()
    {
    }

    public PracticeQuestion(
        Guid id,
        Guid practiceSetId,
        Guid questionId,
        int sortOrder,
        bool isRequired = false,
        QuestionAssignmentSource assignmentSource = QuestionAssignmentSource.Manual)
        : base(id)
    {
        PracticeSetId = practiceSetId;
        QuestionId = questionId;
        AssignmentSource = assignmentSource;
        UpdateDetails(sortOrder, isRequired);
    }

    public void UpdateDetails(int sortOrder, bool isRequired)
    {
        SortOrder = sortOrder;
        IsRequired = isRequired;
    }
}
