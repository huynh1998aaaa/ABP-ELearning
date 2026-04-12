using System.IO;
using System.Threading.Tasks;
using Elearning.Permissions;
using Elearning.Questions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages.Admin.Questions;

[Authorize(ElearningPermissions.Questions.Import)]
public class ImportModel : ElearningAdminPageModel
{
    private readonly IQuestionImportAppService _questionImportAppService;

    public ImportModel(IQuestionImportAppService questionImportAppService)
    {
        _questionImportAppService = questionImportAppService;
    }

    [BindProperty]
    public IFormFile? ImportFile { get; set; }

    public QuestionImportResultDto? Result { get; private set; }

    public async Task<IActionResult> OnGetDownloadTemplateAsync()
    {
        var template = await _questionImportAppService.DownloadTemplateAsync();
        return File(template.Content, template.ContentType, template.FileName);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ImportFile == null || ImportFile.Length == 0)
        {
            ModelState.AddModelError(nameof(ImportFile), L["Questions:Import:FileRequired"]);
            return Page();
        }

        await using var stream = new MemoryStream();
        await ImportFile.CopyToAsync(stream);

        Result = await _questionImportAppService.ImportAsync(new QuestionImportFileDto
        {
            FileName = ImportFile.FileName,
            Content = stream.ToArray()
        });

        return Page();
    }
}
