using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elearning.Permissions;
using Elearning.Practices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages.Admin.Practices;

[Authorize(ElearningPermissions.Practices.ManageQuestions)]
public class QuestionsModel : ElearningAdminPageModel
{
    private readonly IPracticeSetAppService _practiceSetAppService;

    public QuestionsModel(IPracticeSetAppService practiceSetAppService)
    {
        _practiceSetAppService = practiceSetAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? QuestionFilter { get; set; }

    [BindProperty]
    public AddPracticeQuestionDto AddInput { get; set; } = new();

    [BindProperty]
    public AddPracticeQuestionsByCountDto AddByCountInput { get; set; } = new();

    [BindProperty]
    public UpdatePracticeQuestionDto UpdateInput { get; set; } = new();

    public PracticeSetDto PracticeSet { get; private set; } = new();

    public IReadOnlyList<PracticeQuestionDto> PracticeQuestions { get; private set; } = Array.Empty<PracticeQuestionDto>();

    public IReadOnlyList<PracticeAvailableQuestionDto> AvailableQuestions { get; private set; } = Array.Empty<PracticeAvailableQuestionDto>();

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

    public async Task<IActionResult> OnPostAddAllAsync()
    {
        try
        {
            var result = await _practiceSetAppService.AddAllAvailableQuestionsAsync(Id);
            return IsAjaxRequest ? AjaxSuccess(BuildBulkAddMessage(result)) : RedirectToPage(new { Id, QuestionFilter });
        }
        catch (Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }
    }

    public async Task<IActionResult> OnPostAddByCountAsync()
    {
        try
        {
            var result = await _practiceSetAppService.AddQuestionsByCountAsync(Id, AddByCountInput);
            var message = BuildBulkAddMessage(result);
            return IsAjaxRequest ? AjaxSuccess(message) : RedirectToPage(new { Id, QuestionFilter });
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

        return $"/admin/practices/questions/{Id}?{string.Join("&", query)}";
    }

    private async Task LoadAsync()
    {
        PracticeSet = await _practiceSetAppService.GetAsync(Id);
        AddByCountInput.TargetQuestionCount = PracticeSet.TotalQuestionCount;
        PracticeQuestions = (await _practiceSetAppService.GetQuestionsAsync(Id, new GetPracticeQuestionListInput
        {
            MaxResultCount = PracticeSetConsts.MaxQuestionCount,
            SkipCount = 0
        })).Items;

        AvailableQuestions = (await _practiceSetAppService.GetAvailableQuestionsAsync(Id, new GetPracticeAvailableQuestionListInput
        {
            MaxResultCount = 20,
            SkipCount = 0,
            Filter = QuestionFilter
        })).Items;
    }

    private string BuildBulkAddMessage(PracticeBulkAddQuestionsResultDto result)
    {
        if (result.AddedCount == 0 && result.ShortageCount == 0)
        {
            return L["Practices:BulkAddNoAvailableQuestions"];
        }

        return result.ShortageCount > 0
            ? L["Practices:BulkAddAppliedPartial", result.AddedCount, result.TotalAssignedCount, result.ShortageCount]
            : L["Practices:BulkAddApplied", result.AddedCount, result.TotalAssignedCount];
    }
}
