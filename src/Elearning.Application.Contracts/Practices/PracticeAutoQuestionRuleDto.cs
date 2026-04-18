using System;
using Elearning.Questions;
using Volo.Abp.Application.Dtos;

namespace Elearning.Practices;

public class PracticeAutoQuestionRuleDto : FullAuditedEntityDto<Guid>
{
    public Guid PracticeSetId { get; set; }

    public Guid QuestionTypeId { get; set; }

    public string QuestionTypeName { get; set; } = string.Empty;

    public QuestionDifficulty? Difficulty { get; set; }

    public int TargetCount { get; set; }

    public int SortOrder { get; set; }
}
