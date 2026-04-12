using System;

namespace Elearning.Questions;

public class QuestionEssayAnswerDto
{
    public Guid Id { get; set; }

    public string? SampleAnswer { get; set; }

    public string? Rubric { get; set; }

    public int? MaxWords { get; set; }
}
