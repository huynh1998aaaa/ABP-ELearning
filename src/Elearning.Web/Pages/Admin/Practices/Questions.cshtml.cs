using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elearning.Permissions;
using Elearning.Practices;
using Elearning.QuestionTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages.Admin.Practices;

[Authorize(ElearningPermissions.Practices.ManageQuestions)]
public class QuestionsModel : ElearningAdminPageModel
{
    private readonly IPracticeSetAppService _practiceSetAppService;
    private readonly IQuestionTypeAppService _questionTypeAppService;

    public QuestionsModel(
        IPracticeSetAppService practiceSetAppService,
        IQuestionTypeAppService questionTypeAppService)
    {
        _practiceSetAppService = practiceSetAppService;
        _questionTypeAppService = questionTypeAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? QuestionFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool ShowAutoPreview { get; set; }

    [BindProperty]
    public AddPracticeQuestionDto AddInput { get; set; } = new();

    [BindProperty]
    public UpdatePracticeQuestionDto UpdateInput { get; set; } = new();

    [BindProperty]
    public CreatePracticeAutoQuestionRuleDto AutoRuleInput { get; set; } = new()
    {
        TargetCount = 1,
        SortOrder = 10
    };

    [BindProperty]
    public UpdatePracticeAutoQuestionRuleDto UpdateAutoRuleInput { get; set; } = new();

    public PracticeSetDto PracticeSet { get; private set; } = new();

    public IReadOnlyList<PracticeQuestionDto> PracticeQuestions { get; private set; } = Array.Empty<PracticeQuestionDto>();

    public IReadOnlyList<PracticeAvailableQuestionDto> AvailableQuestions { get; private set; } = Array.Empty<PracticeAvailableQuestionDto>();

    public IReadOnlyList<PracticeAutoQuestionRuleDto> AutoQuestionRules { get; private set; } = Array.Empty<PracticeAutoQuestionRuleDto>();

    public PracticeAutoAssignmentPreviewDto? AutoPreview { get; private set; }

    public IReadOnlyList<QuestionTypeDto> QuestionTypes { get; private set; } = Array.Empty<QuestionTypeDto>();

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnGetWorkspaceAsync()
    {
        await LoadAsync();
        return Partial("_QuestionsWorkspace", this);
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        try
        {
            await _practiceSetAppService.AddQuestionAsync(Id, AddInput);
            return IsAjaxRequest ? AjaxSuccess() : RedirectToPage(new { Id, QuestionFilter });
        }
        catch (Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }
    }

    public async Task<IActionResult> OnPostAddRuleAsync()
    {
        try
        {
            await _practiceSetAppService.AddAutoQuestionRuleAsync(Id, AutoRuleInput);
            return IsAjaxRequest ? AjaxSuccess(L["Practices:AutoRuleAdded"]) : RedirectToPage(new { Id, QuestionFilter, ShowAutoPreview });
        }
        catch (Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }
    }

    public async Task<IActionResult> OnPostUpdateRuleAsync(Guid ruleId)
    {
        try
        {
            await _practiceSetAppService.UpdateAutoQuestionRuleAsync(ruleId, UpdateAutoRuleInput);
            return IsAjaxRequest ? AjaxSuccess(L["Practices:AutoRuleUpdated"]) : RedirectToPage(new { Id, QuestionFilter, ShowAutoPreview });
        }
        catch (Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }
    }

    public async Task<IActionResult> OnPostRemoveRuleAsync(Guid ruleId)
    {
        try
        {
            await _practiceSetAppService.RemoveAutoQuestionRuleAsync(ruleId);
            return IsAjaxRequest ? AjaxSuccess(L["Practices:AutoRuleRemoved"]) : RedirectToPage(new { Id, QuestionFilter, ShowAutoPreview });
        }
        catch (Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }
    }

    public async Task<IActionResult> OnPostApplyAutoAsync()
    {
        try
        {
            var result = await _practiceSetAppService.ApplyAutoAssignAsync(Id);
            var message = result.ShortageCount > 0
                ? L["Practices:AutoAssignAppliedPartial", result.AssignedCount, result.RequestedCount, result.ShortageCount]
                : L["Practices:AutoAssignApplied", result.AssignedCount];
            return IsAjaxRequest ? AjaxSuccess(message) : RedirectToPage(new { Id, QuestionFilter, ShowAutoPreview = true });
        }
        catch (Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }
    }

    public async Task<IActionResult> OnPostUpdateAsync(Guid practiceQuestionId)
    {
        try
        {
            await _practiceSetAppService.UpdateQuestionAsync(practiceQuestionId, UpdateInput);
            return IsAjaxRequest ? AjaxSuccess() : RedirectToPage(new { Id, QuestionFilter });
        }
        catch (Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }
    }

    public async Task<IActionResult> OnPostRemoveAsync(Guid practiceQuestionId)
    {
        try
        {
            await _practiceSetAppService.RemoveQuestionAsync(practiceQuestionId);
            return IsAjaxRequest ? AjaxSuccess() : RedirectToPage(new { Id, QuestionFilter });
        }
        catch (Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }
    }

    public string BuildWorkspaceUrl()
    {
        var query = new List<string> { "handler=Workspace" };
        if (!string.IsNullOrWhiteSpace(QuestionFilter))
        {
            query.Add($"questionFilter={Uri.EscapeDataString(QuestionFilter)}");
        }

        if (ShowAutoPreview)
        {
            query.Add("showAutoPreview=true");
        }

        return $"/admin/practices/questions/{Id}?{string.Join("&", query)}";
    }

    private async Task LoadAsync()
    {
        await LoadQuestionTypesAsync();
        PracticeSet = await _practiceSetAppService.GetAsync(Id);
        PracticeQuestions = (await _practiceSetAppService.GetQuestionsAsync(Id, new GetPracticeQuestionListInput
        {
            MaxResultCount = PracticeSetConsts.MaxQuestionCount,
            SkipCount = 0
        })).Items;
        AutoQuestionRules = (await _practiceSetAppService.GetAutoQuestionRulesAsync(Id))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreationTime)
            .ToList();

        AvailableQuestions = (await _practiceSetAppService.GetAvailableQuestionsAsync(Id, new GetPracticeAvailableQuestionListInput
        {
            MaxResultCount = 20,
            SkipCount = 0,
            Filter = QuestionFilter
        })).Items;

        if (ShowAutoPreview)
        {
            AutoPreview = await _practiceSetAppService.PreviewAutoAssignAsync(Id);
        }
    }

    private async Task LoadQuestionTypesAsync()
    {
        QuestionTypes = (await _questionTypeAppService.GetListAsync(new GetQuestionTypeListInput
        {
            MaxResultCount = 1000,
            SkipCount = 0
        })).Items
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .ToList();
    }
}
