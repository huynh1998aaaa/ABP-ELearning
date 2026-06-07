using System.ComponentModel.DataAnnotations;

namespace Elearning.Practices;

public class AddPracticeQuestionsByCountDto
{
    [Range(PracticeSetConsts.MinQuestionCount, PracticeSetConsts.MaxQuestionCount)]
    public int TargetQuestionCount { get; set; } = PracticeSetConsts.MinQuestionCount;
}
