using System;
using System.Threading.Tasks;
using Elearning.Exams;
using Elearning.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages.Admin.Exams;

[Authorize(ElearningPermissions.Exams.Default)]
public class PreviewModel : ElearningAdminPageModel
{
    private readonly IExamAppService _examAppService;

    public PreviewModel(IExamAppService examAppService)
    {
        _examAppService = examAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public ExamPreviewDto Preview { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        Preview = await _examAppService.GetPreviewAsync(Id);
        return Page();
    }
}
