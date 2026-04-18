using System.Collections.Generic;
using System.Linq;

namespace Elearning.Questions;

public class QuestionPdfParseResultDto
{
    public string SourceFileName { get; set; } = string.Empty;

    public int PageCount { get; set; }

    public List<QuestionPdfStagingRowDto> Rows { get; set; } = new();

    public List<string> Errors { get; set; } = new();

    public int TotalRows => Rows.Count;

    public int ReadyCount => Rows.Count(x => x.ParseStatus == QuestionPdfParseStatus.Ready);

    public int NeedsReviewCount => Rows.Count(x => x.ParseStatus == QuestionPdfParseStatus.NeedsReview);

    public int UnsupportedCount => Rows.Count(x => x.ParseStatus == QuestionPdfParseStatus.Unsupported);

    public int InvalidCount => Rows.Count(x => x.ParseStatus == QuestionPdfParseStatus.Invalid);

    public bool HasErrors => Errors.Count > 0;
}
