using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elearning.Permissions;
using Elearning.Subjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages.Admin.Subjects;

[Authorize(ElearningPermissions.Subjects.Default)]
public class IndexModel : ElearningAdminPageModel
{
    private const int PageSize = 10;

    private readonly IAuthorizationService _authorizationService;
    private readonly ISubjectAppService _subjectAppService;

    public IndexModel(
        ISubjectAppService subjectAppService,
        IAuthorizationService authorizationService)
    {
        _subjectAppService = subjectAppService;
        _authorizationService = authorizationService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    public IReadOnlyList<SubjectDto> Subjects { get; private set; } = Array.Empty<SubjectDto>();

    public long TotalCount { get; private set; }

    public int ActiveCount { get; private set; }

    public int InactiveCount { get; private set; }

    public bool CanCreate { get; private set; }

    public bool CanUpdate { get; private set; }

    public bool CanDelete { get; private set; }

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
            await _subjectAppService.ActivateAsync(id);
            if (IsAjaxRequest)
            {
                return AjaxSuccess();
            }
        }
        catch (Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }

        return RedirectToPage(new { Filter, CurrentPage });
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id)
    {
        try
        {
            await _subjectAppService.DeactivateAsync(id);
            if (IsAjaxRequest)
            {
                return AjaxSuccess();
            }
        }
        catch (Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }

        return RedirectToPage(new { Filter, CurrentPage });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _subjectAppService.DeleteAsync(id);
            if (IsAjaxRequest)
            {
                return AjaxSuccess();
            }
        }
        catch (Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }

        return RedirectToPage(new { Filter, CurrentPage });
    }

    public string BuildTableUrl()
    {
        var query = new List<string> { "handler=Table", $"currentPage={CurrentPage}" };
        if (!string.IsNullOrWhiteSpace(Filter))
        {
            query.Add($"filter={Uri.EscapeDataString(Filter)}");
        }

        return $"/admin/subjects?{string.Join("&", query)}";
    }

    public string BuildPageUrl(int page)
    {
        if (string.IsNullOrWhiteSpace(Filter))
        {
            return $"/admin/subjects?currentPage={page}";
        }

        return $"/admin/subjects?filter={Uri.EscapeDataString(Filter)}&currentPage={page}";
    }

    private async Task LoadAsync()
    {
        if (CurrentPage < 1)
        {
            CurrentPage = 1;
        }

        CanCreate = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.Subjects.Create)).Succeeded;
        CanUpdate = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.Subjects.Update)).Succeeded;
        CanDelete = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.Subjects.Delete)).Succeeded;

        var allItems = await _subjectAppService.GetListAsync(new GetSubjectListInput
        {
            MaxResultCount = 1000,
            SkipCount = 0,
            Filter = Filter
        });

        TotalCount = allItems.TotalCount;
        ActiveCount = allItems.Items.Count(x => x.IsActive);
        InactiveCount = allItems.Items.Count(x => !x.IsActive);

        Subjects = allItems.Items
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();
    }
}
