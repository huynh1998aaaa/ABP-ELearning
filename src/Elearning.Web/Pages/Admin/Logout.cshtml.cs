using System.Threading.Tasks;
using Elearning.LoginSessions;
using Elearning.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AbpIdentityUser = Volo.Abp.Identity.IdentityUser;

namespace Elearning.Web.Pages.Admin;

[Authorize]
public class LogoutModel : ElearningPageModel
{
    private readonly SignInManager<AbpIdentityUser> _signInManager;
    private readonly UserLoginSessionManager _userLoginSessionManager;

    public LogoutModel(
        SignInManager<AbpIdentityUser> signInManager,
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
