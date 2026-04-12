using System;

namespace Elearning.Questions;

public class QuestionMatchingPairDto
{
    public Guid Id { get; set; }

    public string LeftText { get; set; } = string.Empty;

    public string RightText { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
