using System;
using System.ComponentModel.DataAnnotations;

namespace Elearning.ClientContent;

public class SaveClientLearningMatchingAnswerDto
{
    [Required]
    public Guid PairId { get; set; }

    public string? SelectedRightText { get; set; }
}
