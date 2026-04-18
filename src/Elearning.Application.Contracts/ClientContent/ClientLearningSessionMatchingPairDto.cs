using System;

namespace Elearning.ClientContent;

public class ClientLearningSessionMatchingPairDto
{
    public Guid Id { get; set; }

    public Guid OriginalMatchingPairId { get; set; }

    public string LeftText { get; set; } = string.Empty;

    public string CorrectRightText { get; set; } = string.Empty;

    public string? SelectedRightText { get; set; }

    public int SortOrder { get; set; }

    public bool IsCorrect { get; set; }
}
