using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elearning.Permissions;
using Elearning.PremiumSubscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Elearning.Web.Pages.Admin.PremiumSubscriptions;

[Authorize(ElearningPermissions.PremiumSubscriptions.Default)]
public class IndexModel : ElearningAdminPageModel
{
    private const int PageSize = 10;

    private readonly IAuthorizationService _authorizationService;
    private readonly IUserPremiumSubscriptionAppService _subscriptionAppService;

    public IndexModel(
        IUserPremiumSubscriptionAppService subscriptionAppService,
        IAuthorizationService authorizationService)
    {
        _subscriptionAppService = subscriptionAppService;
        _authorizationService = authorizationService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty(SupportsGet = true)]
    public PremiumSubscriptionStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    public IReadOnlyList<UserPremiumSubscriptionDto> Subscriptions { get; private set; } = Array.Empty<UserPremiumSubscriptionDto>();

    public List<SelectListItem> StatusOptions { get; private set; } = new();

    public long TotalCount { get; private set; }

    public int ActiveCount { get; private set; }

    public int ExpiredCount { get; private set; }

    public int CancelledCount { get; private set; }

    public bool CanCreate { get; private set; }

    public bool CanUpdate { get; private set; }

    public bool CanCancel { get; private set; }

    public int TotalPages => TotalCount == 0
        ? 1
        : (int)Math.Ceiling((double)TotalCount / PageSize);

    public async Task OnGetAsync()
    {
        if (CurrentPage < 1)
        {
            CurrentPage = 1;
        }

        await LoadPermissionsAsync();
        LoadStatusOptions();

        var allItems = await _subscriptionAppService.GetListAsync(new GetUserPremiumSubscriptionListInput
        {
            MaxResultCount = 1000,
            SkipCount = 0,
            Filter = Filter,
            Status = Status
        });

        TotalCount = allItems.TotalCount;
        ActiveCount = allItems.Items.Count(x => x.Status == PremiumSubscriptionStatus.Active && x.IsCurrentlyActive);
        ExpiredCount = allItems.Items.Count(x => x.Status == PremiumSubscriptionStatus.Expired);
        CancelledCount = allItems.Items.Count(x => x.Status == PremiumSubscriptionStatus.Cancelled);

        Subscriptions = allItems.Items
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();
    }

    public async Task<IActionResult> OnPostExtendAsync(Guid id)
    {
        await _subscriptionAppService.ExtendAsync(id);
        return RedirectToPage(new { Filter, Status, CurrentPage });
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid id)
    {
        await _subscriptionAppService.CancelAsync(id, new CancelPremiumSubscriptionDto());
        return RedirectToPage(new { Filter, Status, CurrentPage });
    }

    public string BuildPageUrl(int page)
    {
        var query = new List<string> { $"currentPage={page}" };
        if (!string.IsNullOrWhiteSpace(Filter))
        {
            query.Add($"filter={Uri.EscapeDataString(Filter)}");
        }

        if (Status.HasValue)
        {
            query.Add($"status={Status.Value}");
        }

        return $"/admin/premiumsubscriptions?{string.Join("&", query)}";
    }

    private async Task LoadPermissionsAsync()
    {
        CanCreate = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.PremiumSubscriptions.Create)).Succeeded;
        CanUpdate = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.PremiumSubscriptions.Update)).Succeeded;
        CanCancel = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.PremiumSubscriptions.Cancel)).Succeeded;
    }

    private void LoadStatusOptions()
    {
        StatusOptions = Enum.GetValues<PremiumSubscriptionStatus>()
            .Select(x => new SelectListItem(L[$"Enum:PremiumSubscriptionStatus:{x}"], x.ToString()))
            .ToList();

        StatusOptions.Insert(0, new SelectListItem(L["PremiumSubscriptions:AllStatuses"], string.Empty));
    }
}
