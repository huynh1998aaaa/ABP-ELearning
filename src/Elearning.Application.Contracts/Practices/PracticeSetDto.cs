using System;
using Volo.Abp.Application.Dtos;

namespace Elearning.Practices;

public class PracticeSetDto : FullAuditedEntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public PracticeStatus Status { get; set; }

    public PracticeAccessLevel AccessLevel { get; set; }

    public PracticeSelectionMode SelectionMode { get; set; }

    public int TotalQuestionCount { get; set; }

    public int AssignedQuestionCount { get; set; }

    public int ValidAssignedQuestionCount { get; set; }

    public int MissingQuestionCount { get; set; }

    public int InvalidQuestionCount { get; set; }

    public bool IsReady { get; set; }

    public bool ShuffleQuestions { get; set; }

    public bool ShuffleOptions { get; set; }

    public bool ShowExplanation { get; set; }

    public bool IsActive { get; set; }

    public int SortOrder { get; set; }

    public DateTime? PublishedTime { get; set; }

    public DateTime? ArchivedTime { get; set; }
}
