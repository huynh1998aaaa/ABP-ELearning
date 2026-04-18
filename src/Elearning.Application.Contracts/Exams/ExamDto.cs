using System;
using Volo.Abp.Application.Dtos;

namespace Elearning.Exams;

public class ExamDto : FullAuditedEntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ExamStatus Status { get; set; }

    public ExamAccessLevel AccessLevel { get; set; }

    public ExamSelectionMode SelectionMode { get; set; }

    public int DurationMinutes { get; set; }

    public int TotalQuestionCount { get; set; }

    public int AssignedQuestionCount { get; set; }

    public decimal? PassingScore { get; set; }

    public bool ShuffleQuestions { get; set; }

    public bool ShuffleOptions { get; set; }

    public bool IsActive { get; set; }

    public int SortOrder { get; set; }

    public DateTime? PublishedTime { get; set; }

    public DateTime? ArchivedTime { get; set; }
}
