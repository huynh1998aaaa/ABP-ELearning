using System.Collections.Generic;

namespace Elearning.Common;

public class BulkDeleteResultDto
{
    public int RequestedCount { get; set; }

    public int SucceededCount { get; set; }

    public int SkippedCount { get; set; }

    public List<string> Errors { get; set; } = new();

    public bool HasErrors => Errors.Count > 0;
}
