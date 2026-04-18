namespace Elearning.Practices;

public class PracticeAutoAssignmentResultDto
{
    public int RequestedCount { get; set; }

    public int FulfilledByManualCount { get; set; }

    public int AssignedCount { get; set; }

    public int ReplacedAutoAssignedCount { get; set; }

    public int PreservedManualCount { get; set; }

    public int ShortageCount { get; set; }
}
