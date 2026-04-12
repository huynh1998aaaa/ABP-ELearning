using System;
using Volo.Abp.Application.Dtos;

namespace Elearning.QuestionTypes;

public class QuestionTypeDto : FullAuditedEntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public QuestionInputKind InputKind { get; set; }

    public QuestionScoringKind ScoringKind { get; set; }

    public bool IsActive { get; set; }

    public bool IsSystem { get; set; }

    public int SortOrder { get; set; }

    public bool SupportsOptions { get; set; }

    public bool SupportsAnswerPairs { get; set; }

    public bool RequiresManualGrading { get; set; }

    public bool AllowMultipleCorrectAnswers { get; set; }

    public int? MinimumOptions { get; set; }

    public int? MaximumOptions { get; set; }
}
