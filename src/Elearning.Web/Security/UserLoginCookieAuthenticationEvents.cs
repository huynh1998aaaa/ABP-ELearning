using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Volo.Abp.DependencyInjection;

namespace Elearning.Web.Security;

public class UserLoginCookieAuthenticationEvents : CookieAuthenticationEvents, ITransientDependency
{
    private readonly UserLoginSessionManager _userLoginSessionManager;

    public UserLoginCookieAuthenticationEvents(UserLoginSessionManager userLoginSessionManager)
    {
        _userLoginSessionManager = userLoginSessionManager;
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        if (context.Principal == null)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return;
        }

        var isValid = await _userLoginSessionManager.ValidatePrincipalAsync(context.HttpContext, context.Principal);
        if (isValid)
        {
            return;
        }

        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
    }
}
