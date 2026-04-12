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

[Authorize(ElearningPermissions.QuestionTypes.Update)]
public class EditModel : ElearningAdminPageModel
{
    private readonly IQuestionTypeAppService _questionTypeAppService;

    public EditModel(IQuestionTypeAppService questionTypeAppService)
    {
        _questionTypeAppService = questionTypeAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public UpdateQuestionTypeDto Input { get; set; } = new();

    public bool IsSystem { get; private set; }

    public bool IsActive { get; private set; }

    public List<SelectListItem> InputKindOptions { get; private set; } = new();

    public List<SelectListItem> ScoringKindOptions { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        LoadOptions();
        await LoadQuestionTypeAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            LoadOptions();
            await LoadFlagsAsync();
            return Page();
        }

        await _questionTypeAppService.UpdateAsync(Id, Input);
        return RedirectToPage("./Index");
    }

    private async Task LoadQuestionTypeAsync()
    {
        var questionType = await _questionTypeAppService.GetAsync(Id);

        Input = new UpdateQuestionTypeDto
        {
            Code = questionType.Code,
            DisplayName = questionType.DisplayName,
            Description = questionType.Description,
            InputKind = questionType.InputKind,
            ScoringKind = questionType.ScoringKind,
            SortOrder = questionType.SortOrder,
            SupportsOptions = questionType.SupportsOptions,
            SupportsAnswerPairs = questionType.SupportsAnswerPairs,
            RequiresManualGrading = questionType.RequiresManualGrading,
            AllowMultipleCorrectAnswers = questionType.AllowMultipleCorrectAnswers,
            MinimumOptions = questionType.MinimumOptions,
            MaximumOptions = questionType.MaximumOptions
        };

        IsSystem = questionType.IsSystem;
        IsActive = questionType.IsActive;
    }

    private async Task LoadFlagsAsync()
    {
        var questionType = await _questionTypeAppService.GetAsync(Id);
        IsSystem = questionType.IsSystem;
        IsActive = questionType.IsActive;
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
