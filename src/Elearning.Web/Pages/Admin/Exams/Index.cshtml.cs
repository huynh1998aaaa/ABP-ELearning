using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elearning.Exams;
using Elearning.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages.Admin.Exams;

[Authorize(ElearningPermissions.Exams.Default)]
public class IndexModel : ElearningAdminPageModel
{
    private const int PageSize = 10;

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
    public ExamStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public ExamAccessLevel? AccessLevel { get; set; }

    [BindProperty(SupportsGet = true)]
    public ExamSelectionMode? SelectionMode { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? IsActive { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    public IReadOnlyList<ExamDto> Exams { get; private set; } = Array.Empty<ExamDto>();

    public long TotalCount { get; private set; }

    public int PublishedCount { get; private set; }

    public int DraftCount { get; private set; }

    public int ArchivedCount { get; private set; }

    public bool CanCreate { get; private set; }

    public bool CanUpdate { get; private set; }

    public bool CanDelete { get; private set; }

    public bool CanPublish { get; private set; }

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

    public async Task<IActionResult> OnPostActivateAsync(Guid id)
    {
        try
        {
            await _examAppService.ActivateAsync(id);
            return IsAjaxRequest ? AjaxSuccess() : RedirectToPage(new { Filter, Status, AccessLevel, SelectionMode, IsActive, CurrentPage });
        }
        catch (Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id)
    {
        try
        {
            await _examAppService.DeactivateAsync(id);
            return IsAjaxRequest ? AjaxSuccess() : RedirectToPage(new { Filter, Status, AccessLevel, SelectionMode, IsActive, CurrentPage });
        }
        catch (Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }
    }

    public async Task<IActionResult> OnPostPublishAsync(Guid id)
    {
        try
        {
            await _examAppService.PublishAsync(id);
            return IsAjaxRequest ? AjaxSuccess() : RedirectToPage(new { Filter, Status, AccessLevel, SelectionMode, IsActive, CurrentPage });
        }
        catch (Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }
    }

    public async Task<IActionResult> OnPostArchiveAsync(Guid id)
    {
        try
        {
            await _examAppService.ArchiveAsync(id);
            return IsAjaxRequest ? AjaxSuccess() : RedirectToPage(new { Filter, Status, AccessLevel, SelectionMode, IsActive, CurrentPage });
        }
        catch (Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _examAppService.DeleteAsync(id);
            return IsAjaxRequest ? AjaxSuccess() : RedirectToPage(new { Filter, Status, AccessLevel, SelectionMode, IsActive, CurrentPage });
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
            MaxResultCount = 1000,
            SkipCount = 0,
            Filter = Filter,
            Status = Status,
            AccessLevel = AccessLevel,
            SelectionMode = SelectionMode,
            IsActive = IsActive
        });

        TotalCount = allItems.TotalCount;
        PublishedCount = allItems.Items.Count(x => x.Status == ExamStatus.Published);
        DraftCount = allItems.Items.Count(x => x.Status == ExamStatus.Draft);
        ArchivedCount = allItems.Items.Count(x => x.Status == ExamStatus.Archived);
        Exams = allItems.Items.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
    }

    private async Task LoadPermissionsAsync()
    {
        CanCreate = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.Exams.Create)).Succeeded;
        CanUpdate = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.Exams.Update)).Succeeded;
        CanDelete = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.Exams.Delete)).Succeeded;
        CanPublish = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.Exams.Publish)).Succeeded;
        CanManageQuestions = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.Exams.ManageQuestions)).Succeeded;
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
        if (Status.HasValue)
        {
            yield return $"status={Status.Value}";
        }
        if (AccessLevel.HasValue)
        {
            yield return $"accessLevel={AccessLevel.Value}";
        }
        if (SelectionMode.HasValue)
        {
            yield return $"selectionMode={SelectionMode.Value}";
        }
        if (IsActive.HasValue)
        {
            yield return $"isActive={IsActive.Value}";
        }
    }
}
