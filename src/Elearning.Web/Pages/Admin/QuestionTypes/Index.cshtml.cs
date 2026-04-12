using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elearning.Permissions;
using Elearning.QuestionTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages.Admin.QuestionTypes;

[Authorize(ElearningPermissions.QuestionTypes.Default)]
public class IndexModel : ElearningAdminPageModel
{
    private const int PageSize = 10;

    private readonly IAuthorizationService _authorizationService;
    private readonly IQuestionTypeAppService _questionTypeAppService;

    public IndexModel(
        IQuestionTypeAppService questionTypeAppService,
        IAuthorizationService authorizationService)
    {
        _questionTypeAppService = questionTypeAppService;
        _authorizationService = authorizationService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    public IReadOnlyList<QuestionTypeDto> QuestionTypes { get; private set; } = Array.Empty<QuestionTypeDto>();

    public long TotalCount { get; private set; }

    public int ActiveCount { get; private set; }

    public int InactiveCount { get; private set; }

    public int SystemCount { get; private set; }

    public bool CanCreate { get; private set; }

    public bool CanUpdate { get; private set; }

    public bool CanDelete { get; private set; }

    public int TotalPages => TotalCount == 0
        ? 1
        : (int)Math.Ceiling((double)TotalCount / PageSize);

    public async Task OnGetAsync()
    {
        if (CurrentPage < 1)
        {
            CurrentPage = 1;
        }

        CanCreate = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.QuestionTypes.Create)).Succeeded;
        CanUpdate = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.QuestionTypes.Update)).Succeeded;
        CanDelete = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.QuestionTypes.Delete)).Succeeded;

        var allItems = await _questionTypeAppService.GetListAsync(new GetQuestionTypeListInput
        {
            MaxResultCount = 1000,
            SkipCount = 0,
            Filter = Filter
        });

        TotalCount = allItems.TotalCount;
        ActiveCount = allItems.Items.Count(x => x.IsActive);
        InactiveCount = allItems.Items.Count(x => !x.IsActive);
        SystemCount = allItems.Items.Count(x => x.IsSystem);

        QuestionTypes = allItems.Items
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();
    }

    public async Task<IActionResult> OnPostActivateAsync(Guid id)
    {
        await _questionTypeAppService.ActivateAsync(id);
        return RedirectToPage(new { Filter, CurrentPage });
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id)
    {
        await _questionTypeAppService.DeactivateAsync(id);
        return RedirectToPage(new { Filter, CurrentPage });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _questionTypeAppService.DeleteAsync(id);
        return RedirectToPage(new { Filter, CurrentPage });
    }

    public string BuildPageUrl(int page)
    {
        if (string.IsNullOrWhiteSpace(Filter))
        {
            return $"/admin/questiontypes?currentPage={page}";
        }

        return $"/admin/questiontypes?filter={Uri.EscapeDataString(Filter)}&currentPage={page}";
    }
}
