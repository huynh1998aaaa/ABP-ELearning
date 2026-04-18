using System.Threading.Tasks;
using Elearning.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages;

public class IndexModel : ElearningPageModel
{
    private readonly IAuthorizationService _authorizationService;

    public IndexModel(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    public bool IsAuthenticated => CurrentUser.IsAuthenticated;

    public string? UserName => CurrentUser.UserName;

    public bool CanAccessAdmin { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (CurrentUser.IsAuthenticated)
        {
            var hasAdminAccess = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.AdminPortal.Access)).Succeeded;
            return Redirect(hasAdminAccess ? "/admin" : "/client");
        }

        CanAccessAdmin = false;
        return Page();
    }
}
