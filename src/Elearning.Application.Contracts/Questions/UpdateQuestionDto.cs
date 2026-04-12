using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Elearning.Questions;

public class UpdateQuestionDto
{
    [Required]
    public Guid QuestionTypeId { get; set; }

    [Required]
    [StringLength(QuestionConsts.MaxTitleLength)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(QuestionConsts.MaxContentLength)]
    public string Content { get; set; } = string.Empty;

    [StringLength(QuestionConsts.MaxExplanationLength)]
    public string? Explanation { get; set; }

    public QuestionDifficulty Difficulty { get; set; }

    [Range(0, 999999)]
    public decimal Score { get; set; }

    public int SortOrder { get; set; }

    public List<QuestionOptionInputDto> Options { get; set; } = new();

    public List<QuestionMatchingPairInputDto> MatchingPairs { get; set; } = new();

    public QuestionEssayAnswerInputDto EssayAnswer { get; set; } = new();
}
