using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Elearning.Permissions;
using Elearning.QuestionTypes;
using Microsoft.AspNetCore.Authorization;
using UglyToad.PdfPig;
using Volo.Abp;
using Volo.Abp.Uow;

namespace Elearning.Questions;

[Authorize(ElearningPermissions.Questions.Import)]
public class QuestionImportAppService : ElearningAppService, IQuestionImportAppService
{
    private const int FirstDataRow = 2;
    private const int MaxChoiceOptions = 6;
    private const int MaxMatchingPairs = 5;

    private readonly IQuestionAppService _questionAppService;
    private readonly IQuestionTypeAppService _questionTypeAppService;

    public QuestionImportAppService(
        IQuestionAppService questionAppService,
        IQuestionTypeAppService questionTypeAppService)
    {
        _questionAppService = questionAppService;
        _questionTypeAppService = questionTypeAppService;
    }

    public Task<QuestionImportTemplateDto> DownloadTemplateAsync()
    {
        using var workbook = new XLWorkbook();

        AddInstructionsSheet(workbook);
        AddChoiceSheet(workbook, "SingleChoice");
        AddChoiceSheet(workbook, "MultipleChoice");
        AddMatchingSheet(workbook);
        AddEssaySheet(workbook);
        AddQuestionTypesSheet(workbook);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return Task.FromResult(new QuestionImportTemplateDto
        {
            FileName = $"question-import-template-{DateTime.UtcNow:yyyyMMdd}.xlsx",
            Content = stream.ToArray()
        });
    }

    [UnitOfWork]
    public async Task<QuestionImportResultDto> ImportAsync(QuestionImportFileDto input)
    {
        if (input.Content.Length == 0 || !input.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return new QuestionImportResultDto
            {
                Errors =
                [
                    new QuestionImportErrorDto
                    {
                        Message = "File import phải là .xlsx và không được rỗng."
                    }
                ]
            };
        }

        var questionTypes = await LoadQuestionTypesByCodeAsync();
        var result = new QuestionImportResultDto();
        var rows = new List<CreateQuestionDto>();

        using var stream = new MemoryStream(input.Content);
        using var workbook = TryOpenWorkbook(stream, result);
        if (workbook == null)
        {
            return result;
        }

        rows.AddRange(ReadChoiceSheet(workbook, "SingleChoice", QuestionTypeCodes.SingleChoice, questionTypes, result));
        rows.AddRange(ReadChoiceSheet(workbook, "MultipleChoice", QuestionTypeCodes.MultipleChoice, questionTypes, result));
        rows.AddRange(ReadMatchingSheet(workbook, questionTypes, result));
        rows.AddRange(ReadEssaySheet(workbook, questionTypes, result));

        if (rows.Count == 0 && !result.HasErrors)
        {
            result.Errors.Add(new QuestionImportErrorDto
            {
                Message = "Không tìm thấy dòng dữ liệu hợp lệ trong file Excel."
            });
        }

        if (result.HasErrors)
        {
            return result;
        }

        foreach (var row in rows)
        {
            await _questionAppService.CreateAsync(row);
            result.ImportedCount++;
        }

        return result;
    }

    public Task<QuestionPdfParseResultDto> PreviewPdfAsync(QuestionImportFileDto input)
    {
        return Task.FromResult(ParsePdfToStagingRows(input));
    }

    public Task<QuestionImportTemplateDto> ConvertPdfToExcelAsync(QuestionImportFileDto input)
    {
        var parseResult = ParsePdfToStagingRows(input);
        using var workbook = new XLWorkbook();

        AddInstructionsSheet(workbook);
        AddChoiceSheet(workbook, "SingleChoice");
        AddChoiceSheet(workbook, "MultipleChoice");
        AddMatchingSheet(workbook);
        AddEssaySheet(workbook);
        AddQuestionTypesSheet(workbook);
        AddPdfReviewQueueSheet(workbook);

        var singleChoiceRow = FirstDataRow;
        var multipleChoiceRow = FirstDataRow;
        var matchingRow = FirstDataRow;
        var essayRow = FirstDataRow;
        var reviewRow = FirstDataRow;

        foreach (var row in parseResult.Rows)
        {
            if (row.ParseStatus != QuestionPdfParseStatus.Ready)
            {
                WriteReviewQueueRow(workbook.Worksheet("PdfReviewQueue"), reviewRow++, row);
                continue;
            }

            if (string.Equals(row.DetectedType, QuestionTypeCodes.SingleChoice, StringComparison.OrdinalIgnoreCase))
            {
                WriteChoiceRow(workbook.Worksheet("SingleChoice"), singleChoiceRow++, row);
                continue;
            }

            if (string.Equals(row.DetectedType, QuestionTypeCodes.MultipleChoice, StringComparison.OrdinalIgnoreCase))
            {
                WriteChoiceRow(workbook.Worksheet("MultipleChoice"), multipleChoiceRow++, row);
                continue;
            }

            if (string.Equals(row.DetectedType, QuestionTypeCodes.Matching, StringComparison.OrdinalIgnoreCase))
            {
                WriteMatchingRow(workbook.Worksheet("Matching"), matchingRow++, row);
                continue;
            }

            if (string.Equals(row.DetectedType, QuestionTypeCodes.Essay, StringComparison.OrdinalIgnoreCase))
            {
                WriteEssayRow(workbook.Worksheet("Essay"), essayRow++, row);
                continue;
            }

            row.ParseStatus = QuestionPdfParseStatus.NeedsReview;
            row.ParseMessage = "Không xác định được loại câu hỏi khi xuất Excel.";
            WriteReviewQueueRow(workbook.Worksheet("PdfReviewQueue"), reviewRow++, row);
        }

        if (parseResult.Errors.Count > 0 && parseResult.Rows.Count == 0)
        {
            WriteReviewQueueRow(workbook.Worksheet("PdfReviewQueue"), reviewRow, new QuestionPdfStagingRowDto
            {
                SourceFileName = input.FileName,
                ParseStatus = QuestionPdfParseStatus.Invalid,
                ParseMessage = string.Join(Environment.NewLine, parseResult.Errors)
            });
        }

        foreach (var sheet in workbook.Worksheets)
        {
            sheet.Columns().AdjustToContents();
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var safeName = Path.GetFileNameWithoutExtension(input.FileName);
        if (safeName.IsNullOrWhiteSpace())
        {
            safeName = "questions-from-pdf";
        }

        return Task.FromResult(new QuestionImportTemplateDto
        {
            FileName = $"{safeName}-normalized-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx",
            Content = stream.ToArray()
        });
    }

    private static XLWorkbook? TryOpenWorkbook(Stream stream, QuestionImportResultDto result)
    {
        try
        {
            return new XLWorkbook(stream);
        }
        catch (Exception)
        {
            AddError(result, string.Empty, 0, string.Empty, "File Excel không đọc được. Vui lòng tải lại template và kiểm tra định dạng .xlsx.");
            return null;
        }
    }

    private static QuestionPdfParseResultDto ParsePdfToStagingRows(QuestionImportFileDto input)
    {
        var result = new QuestionPdfParseResultDto
        {
            SourceFileName = input.FileName
        };

        if (input.Content.Length == 0 || !input.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            result.Errors.Add("File PDF import phải là .pdf và không được rỗng.");
            return result;
        }

        var extractedText = ExtractPdfText(input.Content, result);
        if (extractedText.IsNullOrWhiteSpace())
        {
            result.Errors.Add("Không trích xuất được text từ PDF. File có thể là scan/image hoặc cần OCR.");
            return result;
        }

        var blocks = SplitQuestionBlocks(extractedText);
        if (blocks.Count == 0)
        {
            result.Rows.Add(new QuestionPdfStagingRowDto
            {
                SourceFileName = input.FileName,
                RawSourceText = Truncate(extractedText, 4000),
                ParseStatus = QuestionPdfParseStatus.Invalid,
                ParseMessage = "Không tách được câu hỏi từ text PDF."
            });
            return result;
        }

        foreach (var block in blocks)
        {
            result.Rows.Add(ParseQuestionBlock(input.FileName, block));
        }

        return result;
    }

    private static string ExtractPdfText(byte[] content, QuestionPdfParseResultDto result)
    {
        try
        {
            using var document = PdfDocument.Open(content);
            result.PageCount = document.NumberOfPages;

            var builder = new StringBuilder();
            foreach (var page in document.GetPages())
            {
                var pageText = page.Text ?? string.Empty;
                var wordText = string.Join(" ", page.GetWords().Select(x => x.Text));
                if (wordText.Length > pageText.Length)
                {
                    pageText = wordText;
                }

                if (!pageText.IsNullOrWhiteSpace())
                {
                    builder.AppendLine($"[Page {page.Number}]");
                    builder.AppendLine(pageText);
                    builder.AppendLine();
                }
            }

            return NormalizeExtractedText(builder.ToString());
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Không đọc được PDF: {ex.Message}");
            return string.Empty;
        }
    }

    private static string NormalizeExtractedText(string text)
    {
        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        normalized = Regex.Replace(normalized, @"\bCertify For Sure with IT Exam Dumps\b", string.Empty, RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\bThe No\.1 IT Certification Dumps\s+\d+\b", string.Empty, RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\[\s*Page\s+\d+\s*\]", string.Empty, RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"[ \t]+", " ");
        normalized = Regex.Replace(normalized, @"\n{3,}", "\n\n");
        return normalized.Trim();
    }

    private static List<string> SplitQuestionBlocks(string text)
    {
        var matches = Regex.Matches(
            text,
            @"(?im)\b(?:Question\s*(?:#|No\.?|Number)?\s*\d+|\d{1,5}\.\s*-\s*\(?Topic\s+\d+\)?)(?=\s|$)");

        if (matches.Count == 0)
        {
            return text.IsNullOrWhiteSpace() ? [] : [text];
        }

        var blocks = new List<string>();
        for (var i = 0; i < matches.Count; i++)
        {
            var start = matches[i].Index;
            var end = i + 1 < matches.Count ? matches[i + 1].Index : text.Length;
            var block = text[start..end].Trim();
            if (!block.IsNullOrWhiteSpace())
            {
                blocks.Add(block);
            }
        }

        return blocks;
    }

    private static QuestionPdfStagingRowDto ParseQuestionBlock(string sourceFileName, string block)
    {
        var row = new QuestionPdfStagingRowDto
        {
            SourceFileName = sourceFileName,
            SourceQuestionNo = ExtractQuestionNumber(block),
            RawSourceText = Truncate(block, 4000)
        };

        var unsupportedReason = DetectUnsupportedReason(block);
        var optionMatches = ExtractOptions(block);
        var correctLetters = ExtractCorrectAnswerLetters(block);
        var explanation = ExtractExplanation(block);
        var content = ExtractQuestionContent(block, optionMatches);

        row.Content = Truncate(content, QuestionConsts.MaxContentLength);
        row.Title = BuildTitle(content, row.SourceQuestionNo);
        row.Explanation = Truncate(explanation, QuestionConsts.MaxExplanationLength);
        row.Difficulty = nameof(QuestionDifficulty.Medium);
        row.Score = 1;

        ApplyOptions(row, optionMatches);

        if (!unsupportedReason.IsNullOrWhiteSpace())
        {
            row.ParseStatus = QuestionPdfParseStatus.Unsupported;
            row.ParseMessage = unsupportedReason;
            return row;
        }

        if (content.IsNullOrWhiteSpace())
        {
            row.ParseStatus = QuestionPdfParseStatus.Invalid;
            row.ParseMessage = "Không xác định được nội dung câu hỏi.";
            return row;
        }

        if (content.Length > QuestionConsts.MaxContentLength)
        {
            row.ParseStatus = QuestionPdfParseStatus.Invalid;
            row.ParseMessage = $"Nội dung câu hỏi vượt quá {QuestionConsts.MaxContentLength} ký tự.";
            return row;
        }

        if (optionMatches.Count < 2)
        {
            row.ParseStatus = QuestionPdfParseStatus.NeedsReview;
            row.ParseMessage = "Không parse được tối thiểu 2 option. Cần review thủ công.";
            return row;
        }

        if (correctLetters.Count == 0)
        {
            row.ParseStatus = QuestionPdfParseStatus.NeedsReview;
            row.ParseMessage = "Không tìm thấy đáp án đúng trong PDF. Cần review thủ công.";
            return row;
        }

        var correctIndexes = new List<int>();
        foreach (var letter in correctLetters)
        {
            var optionIndex = optionMatches.FindIndex(x => string.Equals(x.Letter, letter, StringComparison.OrdinalIgnoreCase));
            if (optionIndex < 0)
            {
                row.ParseStatus = QuestionPdfParseStatus.NeedsReview;
                row.ParseMessage = $"Đáp án đúng '{letter}' không khớp với option đã parse.";
                return row;
            }

            correctIndexes.Add(optionIndex + 1);
        }

        row.CorrectAnswers = string.Join(",", correctIndexes.Distinct().OrderBy(x => x));
        row.DetectedType = correctIndexes.Distinct().Count() > 1
            ? QuestionTypeCodes.MultipleChoice
            : QuestionTypeCodes.SingleChoice;
        row.ParseStatus = QuestionPdfParseStatus.Ready;
        row.ParseMessage = "Đã parse được câu hỏi trắc nghiệm và có thể xuất sang Excel import.";
        return row;
    }

    private static int ExtractQuestionNumber(string block)
    {
        var match = Regex.Match(block, @"(?im)\bQuestion\s*(?:#|No\.?|Number)?\s*(\d+)\b");
        if (!match.Success)
        {
            match = Regex.Match(block, @"(?im)\b(\d{1,5})\.\s*-\s*\(?Topic\s+\d+\)?(?=\s|$)");
        }

        return match.Success && int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            ? number
            : 0;
    }

    private static string DetectUnsupportedReason(string block)
    {
        if (Regex.IsMatch(block, @"(?i)\b(hotspot|exhibit|simulation|simlet|testlet|lab)\b"))
        {
            return "Câu hỏi có hotspot/exhibit/simulation/lab nên chưa import tự động trong v1.";
        }

        if (Regex.IsMatch(block, @"(?i)\bdrag\s+and\s+drop\b|\bdrag\s+drop\b"))
        {
            return "Câu hỏi dạng drag/drop cần review thủ công trước khi map sang Matching.";
        }

        return string.Empty;
    }

    private static List<PdfChoiceOption> ExtractOptions(string block)
    {
        var normalized = Regex.Replace(block, @"\s+", " ");
        var matches = Regex.Matches(
            normalized,
            @"(?is)(?:^|\s)([A-F])[\.\)]\s*(.*?)(?=(?:\s+[A-F][\.\)]\s*)|(?:\s+(?:Correct\s+Answers?|Answers?|Explanation|Reference)\b)|$)");

        var options = new List<PdfChoiceOption>();
        foreach (Match match in matches)
        {
            var letter = match.Groups[1].Value.Trim().ToUpperInvariant();
            var text = CleanInlineText(match.Groups[2].Value);
            if (text.IsNullOrWhiteSpace() || options.Any(x => x.Letter == letter))
            {
                continue;
            }

            options.Add(new PdfChoiceOption(letter, Truncate(text, QuestionConsts.MaxOptionTextLength)));
        }

        return options.OrderBy(x => x.Letter).Take(MaxChoiceOptions).ToList();
    }

    private static List<string> ExtractCorrectAnswerLetters(string block)
    {
        var match = Regex.Match(
            block,
            @"(?is)\b(?:Correct\s+Answers?|Answers?)\s*:?\s*([A-F](?:\s*[,;/ ]\s*[A-F])*)\b");
        if (!match.Success)
        {
            return [];
        }

        return Regex.Matches(match.Groups[1].Value, @"[A-F]", RegexOptions.IgnoreCase)
            .Select(x => x.Value.ToUpperInvariant())
            .Distinct()
            .ToList();
    }

    private static string ExtractExplanation(string block)
    {
        var match = Regex.Match(block, @"(?is)\bExplanation(?:/Reference)?\s*:?\s*(.+)$");
        return match.Success ? CleanInlineText(match.Groups[1].Value) : string.Empty;
    }

    private static string ExtractQuestionContent(string block, IReadOnlyList<PdfChoiceOption> options)
    {
        var content = Regex.Replace(block, @"(?im)^\s*\[Page\s+\d+\]\s*$", string.Empty);
        content = Regex.Replace(content, @"(?im)\bQuestion\s*(?:#|No\.?|Number)?\s*\d+\b\.?:?", string.Empty);
        content = Regex.Replace(content, @"(?im)\b\d{1,5}\.\s*-\s*\(?Topic\s+\d+\)?", string.Empty);

        var firstOption = options.Count == 0
            ? null
            : Regex.Match(content, $@"(?is)(?:^|\s){Regex.Escape(options[0].Letter)}[\.\)]\s*{Regex.Escape(options[0].Text[..Math.Min(options[0].Text.Length, 15)])}");
        if (firstOption is { Success: true })
        {
            content = content[..firstOption.Index];
        }
        else
        {
            content = Regex.Split(content, @"(?is)\s+[A-F][\.\)]\s*").FirstOrDefault() ?? content;
        }

        content = Regex.Split(content, @"(?is)\b(?:Correct\s+Answers?|Answers?|Explanation|Reference)\b").FirstOrDefault() ?? content;
        return CleanInlineText(content);
    }

    private static void ApplyOptions(QuestionPdfStagingRowDto row, IReadOnlyList<PdfChoiceOption> options)
    {
        if (options.Count > 0)
        {
            row.Option1 = options[0].Text;
        }

        if (options.Count > 1)
        {
            row.Option2 = options[1].Text;
        }

        if (options.Count > 2)
        {
            row.Option3 = options[2].Text;
        }

        if (options.Count > 3)
        {
            row.Option4 = options[3].Text;
        }

        if (options.Count > 4)
        {
            row.Option5 = options[4].Text;
        }

        if (options.Count > 5)
        {
            row.Option6 = options[5].Text;
        }
    }

    private static string BuildTitle(string content, int sourceQuestionNo)
    {
        var title = content.IsNullOrWhiteSpace()
            ? $"Question {sourceQuestionNo}"
            : content;
        title = CleanInlineText(title);

        if (title.Length > QuestionConsts.MaxTitleLength)
        {
            title = title[..QuestionConsts.MaxTitleLength].Trim();
        }

        return title;
    }

    private static string CleanInlineText(string value)
    {
        return Regex.Replace(value, @"\s+", " ").Trim();
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength].Trim();
    }

    private static void AddPdfReviewQueueSheet(XLWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Add("PdfReviewQueue");
        AddHeaders(sheet,
        [
            "SourceFileName", "SourceQuestionNo", "ParseStatus", "DetectedType", "ParseMessage",
            "Title", "Content", "Explanation",
            "Option1", "Option2", "Option3", "Option4", "Option5", "Option6",
            "CorrectAnswers", "RawSourceText"
        ]);
    }

    private static void WriteChoiceRow(IXLWorksheet sheet, int rowNumber, QuestionPdfStagingRowDto row)
    {
        SetCell(sheet, rowNumber, "Title", row.Title);
        SetCell(sheet, rowNumber, "Content", row.Content);
        SetCell(sheet, rowNumber, "Difficulty", row.Difficulty);
        SetCell(sheet, rowNumber, "Score", row.Score);
        SetCell(sheet, rowNumber, "Explanation", row.Explanation);
        SetCell(sheet, rowNumber, "Option1", row.Option1);
        SetCell(sheet, rowNumber, "Option2", row.Option2);
        SetCell(sheet, rowNumber, "Option3", row.Option3);
        SetCell(sheet, rowNumber, "Option4", row.Option4);
        SetCell(sheet, rowNumber, "Option5", row.Option5);
        SetCell(sheet, rowNumber, "Option6", row.Option6);
        SetCell(sheet, rowNumber, "CorrectAnswers", row.CorrectAnswers);
        SetCell(sheet, rowNumber, "SortOrder", row.SourceQuestionNo);
        SetCell(sheet, rowNumber, "IsActive", true);
    }

    private static void WriteMatchingRow(IXLWorksheet sheet, int rowNumber, QuestionPdfStagingRowDto row)
    {
        SetCell(sheet, rowNumber, "Title", row.Title);
        SetCell(sheet, rowNumber, "Content", row.Content);
        SetCell(sheet, rowNumber, "Difficulty", row.Difficulty);
        SetCell(sheet, rowNumber, "Score", row.Score);
        SetCell(sheet, rowNumber, "Explanation", row.Explanation);
        SetCell(sheet, rowNumber, "Left1", row.Left1);
        SetCell(sheet, rowNumber, "Right1", row.Right1);
        SetCell(sheet, rowNumber, "Left2", row.Left2);
        SetCell(sheet, rowNumber, "Right2", row.Right2);
        SetCell(sheet, rowNumber, "Left3", row.Left3);
        SetCell(sheet, rowNumber, "Right3", row.Right3);
        SetCell(sheet, rowNumber, "Left4", row.Left4);
        SetCell(sheet, rowNumber, "Right4", row.Right4);
        SetCell(sheet, rowNumber, "Left5", row.Left5);
        SetCell(sheet, rowNumber, "Right5", row.Right5);
        SetCell(sheet, rowNumber, "SortOrder", row.SourceQuestionNo);
        SetCell(sheet, rowNumber, "IsActive", true);
    }

    private static void WriteEssayRow(IXLWorksheet sheet, int rowNumber, QuestionPdfStagingRowDto row)
    {
        SetCell(sheet, rowNumber, "Title", row.Title);
        SetCell(sheet, rowNumber, "Content", row.Content);
        SetCell(sheet, rowNumber, "Difficulty", row.Difficulty);
        SetCell(sheet, rowNumber, "Score", row.Score);
        SetCell(sheet, rowNumber, "Explanation", row.Explanation);
        SetCell(sheet, rowNumber, "SampleAnswer", row.SampleAnswer);
        SetCell(sheet, rowNumber, "Rubric", row.Rubric);
        SetCell(sheet, rowNumber, "SortOrder", row.SourceQuestionNo);
        SetCell(sheet, rowNumber, "IsActive", true);
    }

    private static void WriteReviewQueueRow(IXLWorksheet sheet, int rowNumber, QuestionPdfStagingRowDto row)
    {
        SetCell(sheet, rowNumber, "SourceFileName", row.SourceFileName);
        SetCell(sheet, rowNumber, "SourceQuestionNo", row.SourceQuestionNo);
        SetCell(sheet, rowNumber, "ParseStatus", row.ParseStatus.ToString());
        SetCell(sheet, rowNumber, "DetectedType", row.DetectedType);
        SetCell(sheet, rowNumber, "ParseMessage", row.ParseMessage);
        SetCell(sheet, rowNumber, "Title", row.Title);
        SetCell(sheet, rowNumber, "Content", row.Content);
        SetCell(sheet, rowNumber, "Explanation", row.Explanation);
        SetCell(sheet, rowNumber, "Option1", row.Option1);
        SetCell(sheet, rowNumber, "Option2", row.Option2);
        SetCell(sheet, rowNumber, "Option3", row.Option3);
        SetCell(sheet, rowNumber, "Option4", row.Option4);
        SetCell(sheet, rowNumber, "Option5", row.Option5);
        SetCell(sheet, rowNumber, "Option6", row.Option6);
        SetCell(sheet, rowNumber, "CorrectAnswers", row.CorrectAnswers);
        SetCell(sheet, rowNumber, "RawSourceText", row.RawSourceText);
    }

    private static void SetCell<T>(IXLWorksheet sheet, int rowNumber, string columnName, T value)
    {
        var columnNumber = GetColumnNumber(sheet, columnName);
        if (columnNumber.HasValue)
        {
            sheet.Cell(rowNumber, columnNumber.Value).Value = value?.ToString() ?? string.Empty;
        }
    }

    private sealed record PdfChoiceOption(string Letter, string Text);

    private async Task<Dictionary<string, QuestionTypeDto>> LoadQuestionTypesByCodeAsync()
    {
        var questionTypes = await _questionTypeAppService.GetListAsync(new GetQuestionTypeListInput
        {
            MaxResultCount = 1000,
            SkipCount = 0,
            IsActive = true
        });

        return questionTypes.Items.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
    }

    private static void AddInstructionsSheet(XLWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Add("Instructions");
        sheet.Cell(1, 1).Value = "Hướng dẫn import câu hỏi";
        sheet.Cell(2, 1).Value = "Chỉ nhập dữ liệu từ dòng 2. Không đổi tên sheet hoặc tên cột.";
        sheet.Cell(3, 1).Value = "Difficulty nhận Easy, Medium, Hard. IsActive nhận true/false, 1/0 hoặc để trống.";
        sheet.Cell(4, 1).Value = "CorrectAnswers nhập số thứ tự đáp án, ví dụ 1 hoặc 1,3.";
        sheet.Columns().AdjustToContents();
    }

    private static void AddChoiceSheet(XLWorkbook workbook, string sheetName)
    {
        var sheet = workbook.Worksheets.Add(sheetName);
        AddHeaders(sheet, GetChoiceColumns());
    }

    private static void AddMatchingSheet(XLWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Add("Matching");
        AddHeaders(sheet, GetMatchingColumns());
    }

    private static void AddEssaySheet(XLWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Add("Essay");
        AddHeaders(sheet, GetEssayColumns());
    }

    private static void AddQuestionTypesSheet(XLWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Add("QuestionTypes");
        AddHeaders(sheet, ["Code", "Name", "Sheet"]);
        sheet.Cell(2, 1).Value = QuestionTypeCodes.SingleChoice;
        sheet.Cell(2, 2).Value = "Trắc nghiệm 1 đáp án";
        sheet.Cell(2, 3).Value = "SingleChoice";
        sheet.Cell(3, 1).Value = QuestionTypeCodes.MultipleChoice;
        sheet.Cell(3, 2).Value = "Trắc nghiệm nhiều đáp án";
        sheet.Cell(3, 3).Value = "MultipleChoice";
        sheet.Cell(4, 1).Value = QuestionTypeCodes.Matching;
        sheet.Cell(4, 2).Value = "Nối đáp án đúng";
        sheet.Cell(4, 3).Value = "Matching";
        sheet.Cell(5, 1).Value = QuestionTypeCodes.Essay;
        sheet.Cell(5, 2).Value = "Tự luận";
        sheet.Cell(5, 3).Value = "Essay";
        sheet.Columns().AdjustToContents();
    }

    private static void AddHeaders(IXLWorksheet sheet, IReadOnlyList<string> headers)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
            sheet.Cell(1, i + 1).Style.Font.Bold = true;
            sheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();
    }

    private static IReadOnlyList<string> GetChoiceColumns()
    {
        return
        [
            "Title", "Content", "Difficulty", "Score", "Explanation",
            "Option1", "Option2", "Option3", "Option4", "Option5", "Option6",
            "CorrectAnswers", "SortOrder", "IsActive"
        ];
    }

    private static IReadOnlyList<string> GetMatchingColumns()
    {
        var headers = new List<string> { "Title", "Content", "Difficulty", "Score", "Explanation" };
        for (var i = 1; i <= MaxMatchingPairs; i++)
        {
            headers.Add($"Left{i}");
            headers.Add($"Right{i}");
        }

        headers.Add("SortOrder");
        headers.Add("IsActive");
        return headers;
    }

    private static IReadOnlyList<string> GetEssayColumns()
    {
        return ["Title", "Content", "Difficulty", "Score", "Explanation", "SampleAnswer", "Rubric", "MaxWords", "SortOrder", "IsActive"];
    }

    private static bool ValidateRequiredColumns(
        IXLWorksheet sheet,
        IReadOnlyList<string> requiredColumns,
        QuestionImportResultDto result)
    {
        var existingColumns = GetHeaderColumns(sheet);

        foreach (var columnName in requiredColumns)
        {
            if (!existingColumns.Contains(columnName))
            {
                AddError(result, sheet.Name, 1, columnName, $"Thiếu cột bắt buộc '{columnName}'. Vui lòng tải lại template và giữ nguyên tên cột.");
            }
        }

        return requiredColumns.All(existingColumns.Contains);
    }

    private static HashSet<string> GetHeaderColumns(IXLWorksheet sheet)
    {
        var lastColumn = sheet.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0;
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var column = 1; column <= lastColumn; column++)
        {
            var header = sheet.Cell(1, column).GetString().Trim();
            if (!header.IsNullOrWhiteSpace())
            {
                columns.Add(header);
            }
        }

        return columns;
    }

    private static List<CreateQuestionDto> ReadChoiceSheet(
        XLWorkbook workbook,
        string sheetName,
        string questionTypeCode,
        IReadOnlyDictionary<string, QuestionTypeDto> questionTypes,
        QuestionImportResultDto result)
    {
        var rows = new List<CreateQuestionDto>();
        if (!workbook.TryGetWorksheet(sheetName, out var sheet))
        {
            return rows;
        }

        if (!ValidateRequiredColumns(sheet, GetChoiceColumns(), result))
        {
            return rows;
        }

        if (!questionTypes.TryGetValue(questionTypeCode, out var questionType))
        {
            AddError(result, sheetName, 0, "QuestionType", $"Không tìm thấy loại câu hỏi active: {questionTypeCode}.");
            return rows;
        }

        var dataRows = GetUsedDataRows(sheet);
        result.TotalRows += dataRows.Count;

        foreach (var row in dataRows)
        {
            var rowErrorsBefore = result.Errors.Count;
            var options = ReadChoiceOptions(sheet, row.RowNumber(), result);
            var correctIndexes = ParseCorrectAnswers(ReadText(sheet, row.RowNumber(), "CorrectAnswers"), sheetName, row.RowNumber(), result);

            foreach (var correctIndex in correctIndexes)
            {
                if (correctIndex < 1 || correctIndex > options.Count)
                {
                    AddError(result, sheetName, row.RowNumber(), "CorrectAnswers", $"Đáp án đúng {correctIndex} không khớp với số option đã nhập.");
                    continue;
                }

                options[correctIndex - 1].IsCorrect = true;
            }

            var input = ReadBaseQuestion(sheet, row.RowNumber(), questionType.Id, result);
            input.Options = options;
            ValidateChoiceAnswers(sheetName, row.RowNumber(), options, result);

            if (result.Errors.Count == rowErrorsBefore)
            {
                rows.Add(input);
            }
        }

        return rows;
    }

    private static List<CreateQuestionDto> ReadMatchingSheet(
        XLWorkbook workbook,
        IReadOnlyDictionary<string, QuestionTypeDto> questionTypes,
        QuestionImportResultDto result)
    {
        var rows = new List<CreateQuestionDto>();
        const string sheetName = "Matching";
        if (!workbook.TryGetWorksheet(sheetName, out var sheet))
        {
            return rows;
        }

        if (!ValidateRequiredColumns(sheet, GetMatchingColumns(), result))
        {
            return rows;
        }

        if (!questionTypes.TryGetValue(QuestionTypeCodes.Matching, out var questionType))
        {
            AddError(result, sheetName, 0, "QuestionType", $"Không tìm thấy loại câu hỏi active: {QuestionTypeCodes.Matching}.");
            return rows;
        }

        var dataRows = GetUsedDataRows(sheet);
        result.TotalRows += dataRows.Count;

        foreach (var row in dataRows)
        {
            var rowErrorsBefore = result.Errors.Count;
            var input = ReadBaseQuestion(sheet, row.RowNumber(), questionType.Id, result);

            for (var i = 1; i <= MaxMatchingPairs; i++)
            {
                var left = ReadText(sheet, row.RowNumber(), $"Left{i}");
                var right = ReadText(sheet, row.RowNumber(), $"Right{i}");
                if (left.IsNullOrWhiteSpace() && right.IsNullOrWhiteSpace())
                {
                    continue;
                }

                input.MatchingPairs.Add(new QuestionMatchingPairInputDto
                {
                    LeftText = left,
                    RightText = right,
                    SortOrder = i
                });
            }

            ValidateMatchingAnswers(sheetName, row.RowNumber(), input.MatchingPairs, result);

            if (result.Errors.Count == rowErrorsBefore)
            {
                rows.Add(input);
            }
        }

        return rows;
    }

    private static List<CreateQuestionDto> ReadEssaySheet(
        XLWorkbook workbook,
        IReadOnlyDictionary<string, QuestionTypeDto> questionTypes,
        QuestionImportResultDto result)
    {
        var rows = new List<CreateQuestionDto>();
        const string sheetName = "Essay";
        if (!workbook.TryGetWorksheet(sheetName, out var sheet))
        {
            return rows;
        }

        if (!ValidateRequiredColumns(sheet, GetEssayColumns(), result))
        {
            return rows;
        }

        if (!questionTypes.TryGetValue(QuestionTypeCodes.Essay, out var questionType))
        {
            AddError(result, sheetName, 0, "QuestionType", $"Không tìm thấy loại câu hỏi active: {QuestionTypeCodes.Essay}.");
            return rows;
        }

        var dataRows = GetUsedDataRows(sheet);
        result.TotalRows += dataRows.Count;

        foreach (var row in dataRows)
        {
            var rowErrorsBefore = result.Errors.Count;
            var input = ReadBaseQuestion(sheet, row.RowNumber(), questionType.Id, result);
            input.EssayAnswer = new QuestionEssayAnswerInputDto
            {
                SampleAnswer = ReadText(sheet, row.RowNumber(), "SampleAnswer"),
                Rubric = ReadText(sheet, row.RowNumber(), "Rubric"),
                MaxWords = ParseOptionalInt(ReadText(sheet, row.RowNumber(), "MaxWords"), sheetName, row.RowNumber(), "MaxWords", result)
            };
            ValidateTextLength(input.EssayAnswer.SampleAnswer, QuestionConsts.MaxSampleAnswerLength, sheetName, row.RowNumber(), "SampleAnswer", result);
            ValidateTextLength(input.EssayAnswer.Rubric, QuestionConsts.MaxRubricLength, sheetName, row.RowNumber(), "Rubric", result);
            if (input.EssayAnswer.MaxWords.HasValue && input.EssayAnswer.MaxWords < 0)
            {
                AddError(result, sheetName, row.RowNumber(), "MaxWords", "MaxWords phải lớn hơn hoặc bằng 0.");
            }

            if (result.Errors.Count == rowErrorsBefore)
            {
                rows.Add(input);
            }
        }

        return rows;
    }

    private static CreateQuestionDto ReadBaseQuestion(
        IXLWorksheet sheet,
        int rowNumber,
        Guid questionTypeId,
        QuestionImportResultDto result)
    {
        var title = ReadText(sheet, rowNumber, "Title");
        var content = ReadText(sheet, rowNumber, "Content");
        var difficulty = ParseDifficulty(ReadText(sheet, rowNumber, "Difficulty"), sheet.Name, rowNumber, result);
        var score = ParseDecimal(ReadText(sheet, rowNumber, "Score"), sheet.Name, rowNumber, "Score", result) ?? 1;
        var sortOrder = ParseOptionalInt(ReadText(sheet, rowNumber, "SortOrder"), sheet.Name, rowNumber, "SortOrder", result) ?? 0;
        var isActive = ParseOptionalBool(ReadText(sheet, rowNumber, "IsActive"), sheet.Name, rowNumber, result) ?? true;

        if (title.IsNullOrWhiteSpace())
        {
            AddError(result, sheet.Name, rowNumber, "Title", "Title là bắt buộc.");
        }
        ValidateTextLength(title, QuestionConsts.MaxTitleLength, sheet.Name, rowNumber, "Title", result);

        if (content.IsNullOrWhiteSpace())
        {
            AddError(result, sheet.Name, rowNumber, "Content", "Content là bắt buộc.");
        }
        ValidateTextLength(content, QuestionConsts.MaxContentLength, sheet.Name, rowNumber, "Content", result);
        ValidateTextLength(ReadText(sheet, rowNumber, "Explanation"), QuestionConsts.MaxExplanationLength, sheet.Name, rowNumber, "Explanation", result);

        if (score < 0)
        {
            AddError(result, sheet.Name, rowNumber, "Score", "Score phải lớn hơn hoặc bằng 0.");
        }

        return new CreateQuestionDto
        {
            QuestionTypeId = questionTypeId,
            Title = title,
            Content = content,
            Difficulty = difficulty,
            Score = score,
            SortOrder = sortOrder,
            IsActive = isActive,
            Explanation = ReadText(sheet, rowNumber, "Explanation")
        };
    }

    private static List<QuestionOptionInputDto> ReadChoiceOptions(IXLWorksheet sheet, int rowNumber, QuestionImportResultDto result)
    {
        var options = new List<QuestionOptionInputDto>();
        for (var i = 1; i <= MaxChoiceOptions; i++)
        {
            var optionText = ReadText(sheet, rowNumber, $"Option{i}");
            if (optionText.IsNullOrWhiteSpace())
            {
                continue;
            }

            options.Add(new QuestionOptionInputDto
            {
                Text = optionText,
                SortOrder = i
            });
            ValidateTextLength(optionText, QuestionConsts.MaxOptionTextLength, sheet.Name, rowNumber, $"Option{i}", result);
        }

        if (options.Count < 2)
        {
            AddError(result, sheet.Name, rowNumber, "Option1", "Câu hỏi trắc nghiệm cần tối thiểu 2 đáp án.");
        }

        return options;
    }

    private static void ValidateChoiceAnswers(
        string sheetName,
        int rowNumber,
        IReadOnlyList<QuestionOptionInputDto> options,
        QuestionImportResultDto result)
    {
        var correctCount = options.Count(x => x.IsCorrect);
        if (sheetName == "SingleChoice" && correctCount != 1)
        {
            AddError(result, sheetName, rowNumber, "CorrectAnswers", "SingleChoice chỉ được có đúng 1 đáp án đúng.");
        }

        if (sheetName == "MultipleChoice" && correctCount < 1)
        {
            AddError(result, sheetName, rowNumber, "CorrectAnswers", "MultipleChoice cần ít nhất 1 đáp án đúng.");
        }
    }

    private static void ValidateMatchingAnswers(
        string sheetName,
        int rowNumber,
        IReadOnlyList<QuestionMatchingPairInputDto> pairs,
        QuestionImportResultDto result)
    {
        if (pairs.Count < 2)
        {
            AddError(result, sheetName, rowNumber, "Left1", "Matching cần tối thiểu 2 cặp nối đáp án.");
        }

        for (var i = 0; i < pairs.Count; i++)
        {
            var pair = pairs[i];
            var pairNumber = i + 1;
            if (pair.LeftText.IsNullOrWhiteSpace() || pair.RightText.IsNullOrWhiteSpace())
            {
                AddError(result, sheetName, rowNumber, $"Left{pairNumber}/Right{pairNumber}", "Cặp nối đáp án không được thiếu một vế.");
            }

            ValidateTextLength(pair.LeftText, QuestionConsts.MaxMatchingTextLength, sheetName, rowNumber, $"Left{pairNumber}", result);
            ValidateTextLength(pair.RightText, QuestionConsts.MaxMatchingTextLength, sheetName, rowNumber, $"Right{pairNumber}", result);
        }
    }

    private static void ValidateTextLength(
        string? value,
        int maxLength,
        string sheetName,
        int rowNumber,
        string columnName,
        QuestionImportResultDto result)
    {
        if (value != null && value.Length > maxLength)
        {
            AddError(result, sheetName, rowNumber, columnName, $"{columnName} không được vượt quá {maxLength} ký tự.");
        }
    }

    private static List<IXLRow> GetUsedDataRows(IXLWorksheet sheet)
    {
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        var rows = new List<IXLRow>();
        for (var rowNumber = FirstDataRow; rowNumber <= lastRow; rowNumber++)
        {
            var row = sheet.Row(rowNumber);
            if (!row.CellsUsed().Any())
            {
                continue;
            }

            rows.Add(row);
        }

        return rows;
    }

    private static string ReadText(IXLWorksheet sheet, int rowNumber, string columnName)
    {
        var columnNumber = GetColumnNumber(sheet, columnName);
        return columnNumber == null ? string.Empty : sheet.Cell(rowNumber, columnNumber.Value).GetString().Trim();
    }

    private static int? GetColumnNumber(IXLWorksheet sheet, string columnName)
    {
        var lastColumn = sheet.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0;
        for (var column = 1; column <= lastColumn; column++)
        {
            if (string.Equals(sheet.Cell(1, column).GetString().Trim(), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return column;
            }
        }

        return null;
    }

    private static List<int> ParseCorrectAnswers(string value, string sheetName, int rowNumber, QuestionImportResultDto result)
    {
        if (value.IsNullOrWhiteSpace())
        {
            AddError(result, sheetName, rowNumber, "CorrectAnswers", "CorrectAnswers là bắt buộc.");
            return [];
        }

        var answers = new List<int>();
        foreach (var item in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(item, NumberStyles.Integer, CultureInfo.InvariantCulture, out var answer))
            {
                AddError(result, sheetName, rowNumber, "CorrectAnswers", $"Giá trị '{item}' không phải số thứ tự đáp án.");
                continue;
            }

            answers.Add(answer);
        }

        if (sheetName == "SingleChoice" && answers.Count != 1)
        {
            AddError(result, sheetName, rowNumber, "CorrectAnswers", "SingleChoice chỉ được có đúng 1 đáp án đúng.");
        }

        if (sheetName == "MultipleChoice" && answers.Count < 1)
        {
            AddError(result, sheetName, rowNumber, "CorrectAnswers", "MultipleChoice cần ít nhất 1 đáp án đúng.");
        }

        return answers;
    }

    private static QuestionDifficulty ParseDifficulty(string value, string sheetName, int rowNumber, QuestionImportResultDto result)
    {
        if (value.IsNullOrWhiteSpace())
        {
            return QuestionDifficulty.Medium;
        }

        if (Enum.TryParse<QuestionDifficulty>(value, ignoreCase: true, out var difficulty))
        {
            return difficulty;
        }

        AddError(result, sheetName, rowNumber, "Difficulty", "Difficulty chỉ nhận Easy, Medium hoặc Hard.");
        return QuestionDifficulty.Medium;
    }

    private static decimal? ParseDecimal(string value, string sheetName, int rowNumber, string columnName, QuestionImportResultDto result)
    {
        if (value.IsNullOrWhiteSpace())
        {
            return null;
        }

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ||
            decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out parsed))
        {
            return parsed;
        }

        AddError(result, sheetName, rowNumber, columnName, $"{columnName} phải là số.");
        return null;
    }

    private static int? ParseOptionalInt(string value, string sheetName, int rowNumber, string columnName, QuestionImportResultDto result)
    {
        if (value.IsNullOrWhiteSpace())
        {
            return null;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        AddError(result, sheetName, rowNumber, columnName, $"{columnName} phải là số nguyên.");
        return null;
    }

    private static bool? ParseOptionalBool(string value, string sheetName, int rowNumber, QuestionImportResultDto result)
    {
        if (value.IsNullOrWhiteSpace())
        {
            return null;
        }

        if (bool.TryParse(value, out var parsed))
        {
            return parsed;
        }

        if (value == "1")
        {
            return true;
        }

        if (value == "0")
        {
            return false;
        }

        AddError(result, sheetName, rowNumber, "IsActive", "IsActive chỉ nhận true/false, 1/0 hoặc để trống.");
        return null;
    }

    private static void AddError(QuestionImportResultDto result, string sheetName, int rowNumber, string columnName, string message)
    {
        result.Errors.Add(new QuestionImportErrorDto
        {
            SheetName = sheetName,
            RowNumber = rowNumber,
            ColumnName = columnName,
            Message = message
        });
    }
}
