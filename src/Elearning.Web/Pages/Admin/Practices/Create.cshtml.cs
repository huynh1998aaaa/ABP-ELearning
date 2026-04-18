using System.Threading.Tasks;
using Elearning.Permissions;
using Elearning.Practices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages.Admin.Practices;

[Authorize(ElearningPermissions.Practices.Create)]
public class CreateModel : PracticeSetFormPageModel
{
    private readonly IPracticeSetAppService _practiceSetAppService;

    public CreateModel(IPracticeSetAppService practiceSetAppService)
    {
        _practiceSetAppService = practiceSetAppService;
    }

    [BindProperty]
    public CreatePracticeSetDto Input { get; set; } = new();

    public void OnGet()
    {
        LoadSelectOptions();
    }

    public IActionResult OnGetModal()
    {
        LoadSelectOptions();
        return Partial("_CreateForm", this);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            LoadSelectOptions();
            return Page();
        }

        await _practiceSetAppService.CreateAsync(Input);
        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostModalAsync()
    {
        if (!ModelState.IsValid)
        {
            LoadSelectOptions();
            Response.StatusCode = 400;
            return Partial("_CreateForm", this);
        }

        try
        {
            await _practiceSetAppService.CreateAsync(Input);
            return AjaxSuccess();
        }
        catch (System.Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }
    }
}
