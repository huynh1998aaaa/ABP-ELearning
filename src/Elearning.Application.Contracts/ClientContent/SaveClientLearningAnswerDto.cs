using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Elearning.ClientContent;

public class SaveClientLearningAnswerDto
{
    [Required]
    public Guid QuestionId { get; set; }

    public List<Guid> SelectedOptionIds { get; set; } = new();

    public List<SaveClientLearningMatchingAnswerDto> MatchingAnswers { get; set; } = new();

    public string? EssayAnswerText { get; set; }
}
