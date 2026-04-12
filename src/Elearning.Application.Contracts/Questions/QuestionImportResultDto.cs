using System.Collections.Generic;

namespace Elearning.Questions;

public class QuestionImportResultDto
{
    public int TotalRows { get; set; }

    public int ImportedCount { get; set; }

    public List<QuestionImportErrorDto> Errors { get; set; } = new();

    public int ErrorCount => Errors.Count;

    public bool HasErrors => Errors.Count > 0;
}
