using System.Threading.Tasks;
using Elearning.LoginSessions;
using Elearning.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages.Client;

[AllowAnonymous]
public class LogoutModel : ElearningClientPageModel
{
    private readonly SignInManager<Volo.Abp.Identity.IdentityUser> _signInManager;
    private readonly UserLoginSessionManager _userLoginSessionManager;

    public LogoutModel(
        SignInManager<Volo.Abp.Identity.IdentityUser> signInManager,
        UserLoginSessionManager userLoginSessionManager)
    {
        _signInManager = signInManager;
        _userLoginSessionManager = userLoginSessionManager;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await _userLoginSessionManager.RevokeCurrentSessionAsync(
            HttpContext,
            User,
            UserLoginSessionConsts.RevokedBecauseLoggedOut);

        await _signInManager.SignOutAsync();
        return Redirect("~/");
    }
}
