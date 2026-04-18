using System;
using Elearning.Questions;
using Volo.Abp.Application.Dtos;

namespace Elearning.Practices;

public class PracticeQuestionDto : FullAuditedEntityDto<Guid>
{
    public Guid PracticeSetId { get; set; }

    public Guid QuestionId { get; set; }

    public string QuestionTitle { get; set; } = string.Empty;

    public string QuestionContent { get; set; } = string.Empty;

    public string QuestionTypeName { get; set; } = string.Empty;

    public QuestionDifficulty QuestionDifficulty { get; set; }

    public QuestionStatus QuestionStatus { get; set; }

    public bool QuestionIsActive { get; set; }

    public decimal QuestionScore { get; set; }

    public int SortOrder { get; set; }

    public bool IsRequired { get; set; }

    public QuestionAssignmentSource AssignmentSource { get; set; }
}
