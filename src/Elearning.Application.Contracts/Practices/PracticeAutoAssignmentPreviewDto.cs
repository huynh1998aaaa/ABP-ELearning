using System.Collections.Generic;

namespace Elearning.Practices;

public class PracticeAutoAssignmentPreviewDto
{
    public int RequestedCount { get; set; }

    public int FulfilledByManualCount { get; set; }

    public int AutoAssignableCount { get; set; }

    public int ExistingAutoAssignedCount { get; set; }

    public int PreservedManualCount { get; set; }

    public int ShortageCount { get; set; }

    public List<PracticeAutoAssignmentRulePreviewDto> Items { get; set; } = [];
}
