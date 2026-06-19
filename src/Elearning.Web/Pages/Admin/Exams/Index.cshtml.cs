using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elearning.Common;
using Elearning.Exams;
using Elearning.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages.Admin.Exams;

[Authorize(ElearningPermissions.Exams.Default)]
public class IndexModel : ElearningAdminPageModel
{
    private const int PageSize = DefaultAdminPageSize;

    private readonly IAuthorizationService _authorizationService;
    private readonly IExamAppService _examAppService;

    public IndexModel(
        IExamAppService examAppService,
        IAuthorizationService authorizationService)
    {
        _examAppService = examAppService;
        _authorizationService = authorizationService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty(SupportsGet = true)]
    public ExamAccessLevel? AccessLevel { get; set; }

    [BindProperty(SupportsGet = true)]
    public ExamSelectionMode? SelectionMode { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    [BindProperty]
    public BulkDeleteInput BulkDeleteInput { get; set; } = new();

    public IReadOnlyList<ExamDto> Exams { get; private set; } = Array.Empty<ExamDto>();

    public long TotalCount { get; private set; }

    public bool CanCreate { get; private set; }

    public bool CanUpdate { get; private set; }

    public bool CanDelete { get; private set; }

    public bool CanManageQuestions { get; private set; }

    public int TotalPages => TotalCount == 0
        ? 1
        : (int)Math.Ceiling((double)TotalCount / PageSize);

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnGetTableAsync()
    {
        await LoadAsync();
        return Partial("_Table", this);
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _examAppService.DeleteAsync(id);
            return IsAjaxRequest ? AjaxSuccess() : RedirectToPage(new { Filter, AccessLevel, SelectionMode, CurrentPage });
        }
        catch (Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }
    }

    public async Task<IActionResult> OnPostBulkDeleteAsync()
    {
        try
        {
            var result = await _examAppService.BulkDeleteAsync(BulkDeleteInput);
            return IsAjaxRequest
                ? AjaxSuccess(BuildBulkDeleteMessage(result))
                : RedirectToPage(new { Filter, AccessLevel, SelectionMode, CurrentPage });
        }
        catch (Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }
    }

    public string BuildTableUrl()
    {
        return "/admin/exams?" + string.Join("&", BuildQuery("handler=Table", $"currentPage={CurrentPage}"));
    }

    public string BuildPageUrl(int page)
    {
        return "/admin/exams?" + string.Join("&", BuildQuery($"currentPage={page}"));
    }

    private async Task LoadAsync()
    {
        if (CurrentPage < 1)
        {
            CurrentPage = 1;
        }

        await LoadPermissionsAsync();

        var allItems = await _examAppService.GetListAsync(new GetExamListInput
        {
            MaxResultCount = PageSize,
            SkipCount = (CurrentPage - 1) * PageSize,
            Filter = Filter,
            AccessLevel = AccessLevel,
            SelectionMode = SelectionMode
        });

        TotalCount = allItems.TotalCount;
        Exams = allItems.Items.ToList();
    }

    private async Task LoadPermissionsAsync()
    {
        CanCreate = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.Exams.Create)).Succeeded;
        CanUpdate = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.Exams.Update)).Succeeded;
        CanDelete = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.Exams.Delete)).Succeeded;
        CanManageQuestions = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.Exams.ManageQuestions)).Succeeded;
    }

    private string BuildBulkDeleteMessage(BulkDeleteResultDto result)
    {
        return result.HasErrors
            ? L["Exams:BulkDeletePartial", result.SucceededCount, result.SkippedCount]
            : L["Exams:BulkDeleteSuccess", result.SucceededCount, result.SkippedCount];
    }

    private IEnumerable<string> BuildQuery(params string[] firstParts)
    {
        foreach (var part in firstParts)
        {
            yield return part;
        }

        if (!string.IsNullOrWhiteSpace(Filter))
        {
            yield return $"filter={Uri.EscapeDataString(Filter)}";
        }
        if (AccessLevel.HasValue)
        {
            yield return $"accessLevel={AccessLevel.Value}";
        }
        if (SelectionMode.HasValue)
        {
            yield return $"selectionMode={SelectionMode.Value}";
        }
    }
}
