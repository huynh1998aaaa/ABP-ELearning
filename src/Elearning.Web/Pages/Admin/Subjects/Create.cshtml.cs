using System.Threading.Tasks;
using Elearning.Permissions;
using Elearning.Subjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages.Admin.Subjects;

[Authorize(ElearningPermissions.Subjects.Create)]
public class CreateModel : ElearningAdminPageModel
{
    private readonly ISubjectAppService _subjectAppService;

    public CreateModel(ISubjectAppService subjectAppService)
    {
        _subjectAppService = subjectAppService;
    }

    [BindProperty]
    public CreateSubjectDto Input { get; set; } = new()
    {
        SortOrder = 100,
        IsActive = true
    };

    public void OnGet()
    {
    }

    public IActionResult OnGetModal()
    {
        return Partial("_CreateForm", this);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _subjectAppService.CreateAsync(Input);
        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostModalAsync()
    {
        if (!ModelState.IsValid)
        {
            Response.StatusCode = 400;
            return Partial("_CreateForm", this);
        }

        try
        {
            await _subjectAppService.CreateAsync(Input);
            return AjaxSuccess();
        }
        catch (System.Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }
    }
}
