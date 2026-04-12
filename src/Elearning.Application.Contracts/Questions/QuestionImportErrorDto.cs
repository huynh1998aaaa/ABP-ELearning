namespace Elearning.Questions;

public class QuestionImportErrorDto
{
    public string SheetName { get; set; } = string.Empty;

    public int RowNumber { get; set; }

    public string ColumnName { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
