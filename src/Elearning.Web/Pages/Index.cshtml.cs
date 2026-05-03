using System.Threading.Tasks;
using Elearning.Permissions;
using Elearning.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AbpIdentityUser = Volo.Abp.Identity.IdentityUser;

namespace Elearning.Web.Pages;

public class IndexModel : ElearningPageModel
{
    private readonly IAuthorizationService _authorizationService;
    private readonly SignInManager<AbpIdentityUser> _signInManager;

    public IndexModel(
        IAuthorizationService authorizationService,
        SignInManager<AbpIdentityUser> signInManager)
    {
        _authorizationService = authorizationService;
        _signInManager = signInManager;
    }

    public bool IsAuthenticated => CurrentUser.IsAuthenticated;

    public string? UserName => CurrentUser.UserName;

    public bool CanAccessAdmin { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (CurrentUser.IsAuthenticated)
        {
            var hasAdminAccess = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.AdminPortal.Access)).Succeeded;
            if (hasAdminAccess)
            {
                return Redirect("/admin");
            }

            if (ClientAuthenticationConstants.HasClientAccess(User))
            {
                return Redirect("/client");
            }

            await _signInManager.SignOutAsync();
        }

        CanAccessAdmin = false;
        return Page();
    }
}
