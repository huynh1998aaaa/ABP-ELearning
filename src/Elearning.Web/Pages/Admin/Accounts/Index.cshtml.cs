using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Identity;

namespace Elearning.Web.Pages.Admin.Accounts;

[Authorize(IdentityPermissions.Users.Default)]
public class IndexModel : ElearningAdminPageModel
{
    private const int PageSize = 10;
    private const int MaxIdentityQueryResultCount = 1000;
    private readonly IAuthorizationService _authorizationService;
    private readonly IIdentityUserAppService _identityUserAppService;

    public IndexModel(
        IIdentityUserAppService identityUserAppService,
        IAuthorizationService authorizationService)
    {
        _identityUserAppService = identityUserAppService;
        _authorizationService = authorizationService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    public IReadOnlyList<AccountRowViewModel> Users { get; private set; } = Array.Empty<AccountRowViewModel>();

    public long TotalCount { get; private set; }

    public int ActiveCount { get; private set; }

    public int InactiveCount { get; private set; }

    public int AdminCount { get; private set; }

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

        CanCreate = (await _authorizationService.AuthorizeAsync(User, IdentityPermissions.Users.Create)).Succeeded;
        CanUpdate = (await _authorizationService.AuthorizeAsync(User, IdentityPermissions.Users.Update)).Succeeded;
        CanDelete = (await _authorizationService.AuthorizeAsync(User, IdentityPermissions.Users.Delete)).Succeeded;

        var allUsers = await _identityUserAppService.GetListAsync(new GetIdentityUsersInput
        {
            MaxResultCount = MaxIdentityQueryResultCount,
            SkipCount = 0,
            Filter = Filter
        });

        TotalCount = allUsers.TotalCount;
        ActiveCount = allUsers.Items.Count(x => x.IsActive);
        InactiveCount = allUsers.Items.Count(x => !x.IsActive);
        AdminCount = allUsers.Items.Count(x =>
            string.Equals(x.UserName, "admin", StringComparison.OrdinalIgnoreCase));

        Users = allUsers.Items
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .Select(x => new AccountRowViewModel
            {
                Id = x.Id,
                UserName = x.UserName ?? string.Empty,
                DisplayName = $"{x.Name} {x.Surname}".Trim(),
                Email = x.Email ?? string.Empty,
                PhoneNumber = x.PhoneNumber ?? string.Empty,
                IsActive = x.IsActive,
                CreationTime = x.CreationTime
            })
            .ToList();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        if (!(await _authorizationService.AuthorizeAsync(User, IdentityPermissions.Users.Delete)).Succeeded)
        {
            return Forbid();
        }

        await _identityUserAppService.DeleteAsync(id);
        return RedirectToPage(new { Filter, CurrentPage });
    }

    public string BuildPageUrl(int page)
    {
        if (string.IsNullOrWhiteSpace(Filter))
        {
            return $"/admin/accounts?currentPage={page}";
        }

        return $"/admin/accounts?filter={Uri.EscapeDataString(Filter)}&currentPage={page}";
    }

    public class AccountRowViewModel
    {
        public Guid Id { get; init; }

        public string UserName { get; init; } = string.Empty;

        public string DisplayName { get; init; } = string.Empty;

        public string Email { get; init; } = string.Empty;

        public string PhoneNumber { get; init; } = string.Empty;

        public bool IsActive { get; init; }

        public DateTime CreationTime { get; init; }
    }
}
