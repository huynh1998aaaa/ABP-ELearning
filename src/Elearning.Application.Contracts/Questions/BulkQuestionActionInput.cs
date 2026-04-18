using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Elearning.Questions;

public class BulkQuestionActionInput
{
    [Required]
    [MinLength(1)]
    public List<Guid> QuestionIds { get; set; } = new();
}
