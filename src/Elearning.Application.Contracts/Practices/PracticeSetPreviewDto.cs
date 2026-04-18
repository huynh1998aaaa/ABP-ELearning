using System.Collections.Generic;

namespace Elearning.Practices;

public class PracticeSetPreviewDto
{
    public PracticeSetDto PracticeSet { get; set; } = new();

    public List<PracticeQuestionDto> Questions { get; set; } = new();
}
