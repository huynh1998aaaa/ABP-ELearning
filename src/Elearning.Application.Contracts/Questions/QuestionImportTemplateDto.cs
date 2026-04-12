namespace Elearning.Questions;

public class QuestionImportTemplateDto
{
    public string FileName { get; set; } = "question-import-template.xlsx";

    public string ContentType { get; set; } = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public byte[] Content { get; set; } = [];
}
