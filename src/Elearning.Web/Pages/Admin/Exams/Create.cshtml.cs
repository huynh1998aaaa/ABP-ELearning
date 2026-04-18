using System.Threading.Tasks;
using Elearning.Exams;
using Elearning.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages.Admin.Exams;

[Authorize(ElearningPermissions.Exams.Create)]
public class CreateModel : ExamFormPageModel
{
    private readonly IExamAppService _examAppService;

    public CreateModel(IExamAppService examAppService)
    {
        _examAppService = examAppService;
    }

    [BindProperty]
    public CreateExamDto Input { get; set; } = new()
    {
        AccessLevel = ExamAccessLevel.Free,
        SelectionMode = ExamSelectionMode.Fixed,
        DurationMinutes = 60,
        TotalQuestionCount = 1,
        SortOrder = 100,
        IsActive = true
    };

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

        await _examAppService.CreateAsync(Input);
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
            await _examAppService.CreateAsync(Input);
            return AjaxSuccess();
        }
        catch (System.Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }
    }
}
