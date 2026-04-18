using System;

namespace Elearning.ClientContent;

public class ClientLearningSessionOptionDto
{
    public Guid Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsSelected { get; set; }

    public bool IsCorrect { get; set; }
}
