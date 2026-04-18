using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Elearning.Questions;

public interface IQuestionImportAppService : IApplicationService
{
    Task<QuestionImportTemplateDto> DownloadTemplateAsync();

    Task<QuestionImportResultDto> ImportAsync(QuestionImportFileDto input);

    Task<QuestionPdfParseResultDto> PreviewPdfAsync(QuestionImportFileDto input);

    Task<QuestionImportTemplateDto> ConvertPdfToExcelAsync(QuestionImportFileDto input);
}
