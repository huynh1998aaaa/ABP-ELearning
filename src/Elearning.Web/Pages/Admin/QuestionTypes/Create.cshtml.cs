using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elearning.Permissions;
using Elearning.QuestionTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Elearning.Web.Pages.Admin.QuestionTypes;

[Authorize(ElearningPermissions.QuestionTypes.Create)]
public class CreateModel : ElearningAdminPageModel
{
    private readonly IQuestionTypeAppService _questionTypeAppService;

    public CreateModel(IQuestionTypeAppService questionTypeAppService)
    {
        _questionTypeAppService = questionTypeAppService;
    }

    [BindProperty]
    public CreateQuestionTypeDto Input { get; set; } = new()
    {
        InputKind = QuestionInputKind.SingleChoice,
        ScoringKind = QuestionScoringKind.Auto,
        SupportsOptions = true,
        MinimumOptions = 2,
        SortOrder = 100,
        IsActive = true
    };

    public List<SelectListItem> InputKindOptions { get; private set; } = new();

    public List<SelectListItem> ScoringKindOptions { get; private set; } = new();

    public void OnGet()
    {
        LoadOptions();
    }

    public IActionResult OnGetModal()
    {
        LoadOptions();
        return Partial("_CreateForm", this);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            LoadOptions();
            return Page();
        }

        await _questionTypeAppService.CreateAsync(Input);
        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostModalAsync()
    {
        if (!ModelState.IsValid)
        {
            LoadOptions();
            Response.StatusCode = 400;
            return Partial("_CreateForm", this);
        }

        try
        {
            await _questionTypeAppService.CreateAsync(Input);
            return AjaxSuccess();
        }
        catch (System.Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }
    }

    private void LoadOptions()
    {
        InputKindOptions = Enum.GetValues<QuestionInputKind>()
            .Select(x => new SelectListItem(L[$"Enum:QuestionInputKind:{x}"], x.ToString()))
            .ToList();

        ScoringKindOptions = Enum.GetValues<QuestionScoringKind>()
            .Select(x => new SelectListItem(L[$"Enum:QuestionScoringKind:{x}"], x.ToString()))
            .ToList();
    }
}
