using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Elearning.Permissions;
using Elearning.QuestionTypes;
using Microsoft.AspNetCore.Authorization;
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
