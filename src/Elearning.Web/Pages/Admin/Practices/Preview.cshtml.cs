using System;
using System.Threading.Tasks;
using Elearning.Permissions;
using Elearning.Practices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages.Admin.Practices;

[Authorize(ElearningPermissions.Practices.Default)]
public class PreviewModel : ElearningAdminPageModel
{
    private readonly IPracticeSetAppService _practiceSetAppService;

    public PreviewModel(IPracticeSetAppService practiceSetAppService)
    {
        _practiceSetAppService = practiceSetAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public PracticeSetPreviewDto Preview { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        Preview = await _practiceSetAppService.GetPreviewAsync(Id);
        return Page();
    }
}
