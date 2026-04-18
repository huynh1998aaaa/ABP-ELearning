using System.Collections.Generic;

namespace Elearning.Exams;

public class ExamPreviewDto
{
    public ExamDto Exam { get; set; } = new();

    public List<ExamQuestionDto> Questions { get; set; } = new();
}
