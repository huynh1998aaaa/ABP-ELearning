using System.ComponentModel.DataAnnotations;

namespace Elearning.Exams;

public class AddExamQuestionsByCountDto
{
    [Range(ExamConsts.MinQuestionCount, ExamConsts.MaxQuestionCount)]
    public int TargetQuestionCount { get; set; } = ExamConsts.MinQuestionCount;
}
