using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace Elearning.Questions;

public class QuestionDto : FullAuditedEntityDto<Guid>
{
    public Guid QuestionTypeId { get; set; }

    public string QuestionTypeCode { get; set; } = string.Empty;

    public string QuestionTypeName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string? Explanation { get; set; }

    public QuestionDifficulty Difficulty { get; set; }

    public decimal Score { get; set; }

    public bool IsActive { get; set; }

    public int SortOrder { get; set; }

    public QuestionStatus Status { get; set; }

    public List<QuestionOptionDto> Options { get; set; } = new();

    public List<QuestionMatchingPairDto> MatchingPairs { get; set; } = new();

    public QuestionEssayAnswerDto? EssayAnswer { get; set; }
}
