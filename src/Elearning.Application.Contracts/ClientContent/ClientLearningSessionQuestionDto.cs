using System;
using System.Collections.Generic;

namespace Elearning.ClientContent;

public class ClientLearningSessionQuestionDto
{
    public Guid Id { get; set; }

    public Guid OriginalQuestionId { get; set; }

    public string QuestionTypeCode { get; set; } = string.Empty;

    public string QuestionTypeName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string? Explanation { get; set; }

    public int SortOrder { get; set; }

    public decimal Score { get; set; }

    public bool IsAnswered { get; set; }

    public bool IsCorrect { get; set; }

    public List<Guid> SelectedOptionIds { get; set; } = new();

    public List<ClientLearningSessionOptionDto> Options { get; set; } = new();

    public List<string> MatchingChoices { get; set; } = new();

    public List<ClientLearningSessionMatchingPairDto> MatchingPairs { get; set; } = new();

    public string? EssayAnswerText { get; set; }

    public string? EssaySampleAnswer { get; set; }

    public string? EssayRubric { get; set; }

    public int? EssayMaxWords { get; set; }
}
