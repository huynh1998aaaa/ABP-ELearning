using System;
using System.ComponentModel.DataAnnotations;

namespace Elearning.Practices;

public class AddPracticeQuestionDto
{
    [Required]
    public Guid QuestionId { get; set; }

    public int SortOrder { get; set; }

    public bool IsRequired { get; set; }
}
