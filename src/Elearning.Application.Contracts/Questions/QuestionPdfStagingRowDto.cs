namespace Elearning.Questions;

public class QuestionPdfStagingRowDto
{
    public string SourceFileName { get; set; } = string.Empty;

    public int SourceQuestionNo { get; set; }

    public string DetectedType { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string Difficulty { get; set; } = "Medium";

    public decimal Score { get; set; } = 1;

    public string Explanation { get; set; } = string.Empty;

    public string Option1 { get; set; } = string.Empty;

    public string Option2 { get; set; } = string.Empty;

    public string Option3 { get; set; } = string.Empty;

    public string Option4 { get; set; } = string.Empty;

    public string Option5 { get; set; } = string.Empty;

    public string Option6 { get; set; } = string.Empty;

    public string CorrectAnswers { get; set; } = string.Empty;

    public string Left1 { get; set; } = string.Empty;

    public string Right1 { get; set; } = string.Empty;

    public string Left2 { get; set; } = string.Empty;

    public string Right2 { get; set; } = string.Empty;

    public string Left3 { get; set; } = string.Empty;

    public string Right3 { get; set; } = string.Empty;

    public string Left4 { get; set; } = string.Empty;

    public string Right4 { get; set; } = string.Empty;

    public string Left5 { get; set; } = string.Empty;

    public string Right5 { get; set; } = string.Empty;

    public string SampleAnswer { get; set; } = string.Empty;

    public string Rubric { get; set; } = string.Empty;

    public string RawSourceText { get; set; } = string.Empty;

    public QuestionPdfParseStatus ParseStatus { get; set; } = QuestionPdfParseStatus.NeedsReview;

    public string ParseMessage { get; set; } = string.Empty;

    public bool NeedsReview => ParseStatus != QuestionPdfParseStatus.Ready;
}
