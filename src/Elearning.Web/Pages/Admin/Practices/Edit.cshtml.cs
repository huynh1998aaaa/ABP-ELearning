using System;
using System.Threading.Tasks;
using Elearning.Permissions;
using Elearning.Practices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages.Admin.Practices;

[Authorize(ElearningPermissions.Practices.Update)]
public class EditModel : PracticeSetFormPageModel
{
    private readonly IPracticeSetAppService _practiceSetAppService;

    public EditModel(IPracticeSetAppService practiceSetAppService)
    {
        _practiceSetAppService = practiceSetAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public UpdatePracticeSetDto Input { get; set; } = new();

    public PracticeSetDto PracticeSet { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        LoadSelectOptions();
        await LoadPracticeSetAsync();
        return Page();
    }

    public async Task<IActionResult> OnGetModalAsync()
    {
        LoadSelectOptions();
        await LoadPracticeSetAsync();
        return Partial("_EditForm", this);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            LoadSelectOptions();
            PracticeSet = await _practiceSetAppService.GetAsync(Id);
            return Page();
        }

        await _practiceSetAppService.UpdateAsync(Id, Input);
        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostModalAsync()
    {
        if (!ModelState.IsValid)
        {
            LoadSelectOptions();
            PracticeSet = await _practiceSetAppService.GetAsync(Id);
            Response.StatusCode = 400;
            return Partial("_EditForm", this);
        }

        try
        {
            await _practiceSetAppService.UpdateAsync(Id, Input);
            return AjaxSuccess();
        }
        catch (System.Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }
    }

    private async Task LoadPracticeSetAsync()
    {
        PracticeSet = await _practiceSetAppService.GetAsync(Id);
        Input = new UpdatePracticeSetDto
        {
            Code = PracticeSet.Code,
            Title = PracticeSet.Title,
            Description = PracticeSet.Description,
            AccessLevel = PracticeSet.AccessLevel,
            SelectionMode = PracticeSet.SelectionMode,
            TotalQuestionCount = PracticeSet.TotalQuestionCount,
            ShuffleQuestions = PracticeSet.ShuffleQuestions,
            ShowExplanation = PracticeSet.ShowExplanation,
            SortOrder = PracticeSet.SortOrder
        };
    }
}
