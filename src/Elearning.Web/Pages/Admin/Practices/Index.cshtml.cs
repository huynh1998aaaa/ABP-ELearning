using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elearning.Permissions;
using Elearning.Practices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages.Admin.Practices;

[Authorize(ElearningPermissions.Practices.Default)]
public class IndexModel : ElearningAdminPageModel
{
    private const int PageSize = 10;

    private readonly IAuthorizationService _authorizationService;
    private readonly IPracticeSetAppService _practiceSetAppService;

    public IndexModel(
        IPracticeSetAppService practiceSetAppService,
        IAuthorizationService authorizationService)
    {
        _practiceSetAppService = practiceSetAppService;
        _authorizationService = authorizationService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty(SupportsGet = true)]
    public PracticeStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public PracticeAccessLevel? AccessLevel { get; set; }

    [BindProperty(SupportsGet = true)]
    public PracticeSelectionMode? SelectionMode { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    public IReadOnlyList<PracticeSetDto> PracticeSets { get; private set; } = Array.Empty<PracticeSetDto>();

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
            await _practiceSetAppService.ActivateAsync(id);
            return IsAjaxRequest ? AjaxSuccess() : RedirectToPage(new { Filter, Status, AccessLevel, SelectionMode, CurrentPage });
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
            await _practiceSetAppService.DeactivateAsync(id);
            return IsAjaxRequest ? AjaxSuccess() : RedirectToPage(new { Filter, Status, AccessLevel, SelectionMode, CurrentPage });
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
            await _practiceSetAppService.PublishAsync(id);
            return IsAjaxRequest ? AjaxSuccess() : RedirectToPage(new { Filter, Status, AccessLevel, SelectionMode, CurrentPage });
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
            await _practiceSetAppService.ArchiveAsync(id);
            return IsAjaxRequest ? AjaxSuccess() : RedirectToPage(new { Filter, Status, AccessLevel, SelectionMode, CurrentPage });
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
            await _practiceSetAppService.DeleteAsync(id);
            return IsAjaxRequest ? AjaxSuccess() : RedirectToPage(new { Filter, Status, AccessLevel, SelectionMode, CurrentPage });
        }
        catch (Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }
    }

    public string BuildTableUrl()
    {
        return "/admin/practices?" + string.Join("&", BuildQuery("handler=Table", $"currentPage={CurrentPage}"));
    }

    public string BuildPageUrl(int page)
    {
        return "/admin/practices?" + string.Join("&", BuildQuery($"currentPage={page}"));
    }

    private async Task LoadAsync()
    {
        if (CurrentPage < 1)
        {
            CurrentPage = 1;
        }

        await LoadPermissionsAsync();

        var allItems = await _practiceSetAppService.GetListAsync(new GetPracticeSetListInput
        {
            MaxResultCount = 1000,
            SkipCount = 0,
            Filter = Filter,
            Status = Status,
            AccessLevel = AccessLevel,
            SelectionMode = SelectionMode
        });

        TotalCount = allItems.TotalCount;
        PublishedCount = allItems.Items.Count(x => x.Status == PracticeStatus.Published);
        DraftCount = allItems.Items.Count(x => x.Status == PracticeStatus.Draft);
        ArchivedCount = allItems.Items.Count(x => x.Status == PracticeStatus.Archived);
        PracticeSets = allItems.Items.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
    }

    private async Task LoadPermissionsAsync()
    {
        CanCreate = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.Practices.Create)).Succeeded;
        CanUpdate = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.Practices.Update)).Succeeded;
        CanDelete = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.Practices.Delete)).Succeeded;
        CanPublish = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.Practices.Publish)).Succeeded;
        CanManageQuestions = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.Practices.ManageQuestions)).Succeeded;
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
    }
}
