using System;
using Volo.Abp.Application.Dtos;

namespace Elearning.Questions;

public class GetQuestionListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }

    public Guid? QuestionTypeId { get; set; }

    public QuestionDifficulty? Difficulty { get; set; }

    public QuestionStatus? Status { get; set; }

    public bool? IsActive { get; set; }
}
