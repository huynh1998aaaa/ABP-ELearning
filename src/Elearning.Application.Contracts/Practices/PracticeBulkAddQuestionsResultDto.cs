namespace Elearning.Practices;

public class PracticeBulkAddQuestionsResultDto
{
    public int RequestedCount { get; set; }

    public int AddedCount { get; set; }

    public int AlreadyAssignedCount { get; set; }

    public int TotalAssignedCount { get; set; }

    public int ShortageCount { get; set; }
}
