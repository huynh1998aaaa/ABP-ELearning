using System;

namespace Elearning.ClientContent;

public class ClientLearningItemDto
{
    public Guid Id { get; set; }

    public ClientLearningItemKind Kind { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ClientLearningAccessLevel AccessLevel { get; set; }

    public ClientLearningSelectionMode SelectionMode { get; set; }

    public int TotalQuestionCount { get; set; }

    public int AssignedQuestionCount { get; set; }

    public bool IsLocked { get; set; }

    public bool ShowExplanation { get; set; }

    public int SortOrder { get; set; }
}
