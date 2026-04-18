using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elearning.Exams;
using Elearning.Permissions;
using Elearning.QuestionTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages.Admin.Exams;

[Authorize(ElearningPermissions.Exams.ManageQuestions)]
public class QuestionsModel : ElearningAdminPageModel
{
    private readonly IExamAppService _examAppService;
    private readonly IQuestionTypeAppService _questionTypeAppService;

    public QuestionsModel(
        IExamAppService examAppService,
        IQuestionTypeAppService questionTypeAppService)
    {
        _examAppService = examAppService;
        _questionTypeAppService = questionTypeAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? QuestionFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool ShowAutoPreview { get; set; }

    [BindProperty]
    public AddExamQuestionDto AddInput { get; set; } = new();

    [BindProperty]
    public UpdateExamQuestionDto UpdateInput { get; set; } = new();

    [BindProperty]
    public CreateExamAutoQuestionRuleDto AutoRuleInput { get; set; } = new()
    {
        TargetCount = 1,
        SortOrder = 10
    };

    [BindProperty]
    public UpdateExamAutoQuestionRuleDto UpdateAutoRuleInput { get; set; } = new();

    public ExamDto Exam { get; private set; } = new();

    public IReadOnlyList<ExamQuestionDto> ExamQuestions { get; private set; } = Array.Empty<ExamQuestionDto>();

    public IReadOnlyList<ExamAvailableQuestionDto> AvailableQuestions { get; private set; } = Array.Empty<ExamAvailableQuestionDto>();

    public IReadOnlyList<ExamAutoQuestionRuleDto> AutoQuestionRules { get; private set; } = Array.Empty<ExamAutoQuestionRuleDto>();

    public ExamAutoAssignmentPreviewDto? AutoPreview { get; private set; }

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
            await _examAppService.AddQuestionAsync(Id, AddInput);
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
            await _examAppService.AddAutoQuestionRuleAsync(Id, AutoRuleInput);
            return IsAjaxRequest ? AjaxSuccess(L["Exams:AutoRuleAdded"]) : RedirectToPage(new { Id, QuestionFilter, ShowAutoPreview });
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
            await _examAppService.UpdateAutoQuestionRuleAsync(ruleId, UpdateAutoRuleInput);
            return IsAjaxRequest ? AjaxSuccess(L["Exams:AutoRuleUpdated"]) : RedirectToPage(new { Id, QuestionFilter, ShowAutoPreview });
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
            await _examAppService.RemoveAutoQuestionRuleAsync(ruleId);
            return IsAjaxRequest ? AjaxSuccess(L["Exams:AutoRuleRemoved"]) : RedirectToPage(new { Id, QuestionFilter, ShowAutoPreview });
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
            var result = await _examAppService.ApplyAutoAssignAsync(Id);
            var message = result.ShortageCount > 0
                ? L["Exams:AutoAssignAppliedPartial", result.AssignedCount, result.RequestedCount, result.ShortageCount]
                : L["Exams:AutoAssignApplied", result.AssignedCount];
            return IsAjaxRequest ? AjaxSuccess(message) : RedirectToPage(new { Id, QuestionFilter, ShowAutoPreview = true });
        }
        catch (Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }
    }

    public async Task<IActionResult> OnPostUpdateAsync(Guid examQuestionId)
    {
        try
        {
            await _examAppService.UpdateQuestionAsync(examQuestionId, UpdateInput);
            return IsAjaxRequest ? AjaxSuccess() : RedirectToPage(new { Id, QuestionFilter });
        }
        catch (Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }
    }

    public async Task<IActionResult> OnPostRemoveAsync(Guid examQuestionId)
    {
        try
        {
            await _examAppService.RemoveQuestionAsync(examQuestionId);
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

        return $"/admin/exams/questions/{Id}?{string.Join("&", query)}";
    }

    private async Task LoadAsync()
    {
        await LoadQuestionTypesAsync();
        Exam = await _examAppService.GetAsync(Id);
        ExamQuestions = (await _examAppService.GetQuestionsAsync(Id, new GetExamQuestionListInput
        {
            MaxResultCount = ExamConsts.MaxQuestionCount,
            SkipCount = 0
        })).Items;
        AutoQuestionRules = (await _examAppService.GetAutoQuestionRulesAsync(Id))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreationTime)
            .ToList();

        AvailableQuestions = (await _examAppService.GetAvailableQuestionsAsync(Id, new GetExamAvailableQuestionListInput
        {
            MaxResultCount = 20,
            SkipCount = 0,
            Filter = QuestionFilter
        })).Items;

        if (ShowAutoPreview)
        {
            AutoPreview = await _examAppService.PreviewAutoAssignAsync(Id);
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
