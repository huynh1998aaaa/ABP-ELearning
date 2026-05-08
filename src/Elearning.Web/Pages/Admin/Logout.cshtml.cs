using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages.Admin;

[AllowAnonymous]
public class LogoutModel : ElearningPageModel
{
    private readonly SignInManager<Volo.Abp.Identity.IdentityUser> _signInManager;

    public LogoutModel(SignInManager<Volo.Abp.Identity.IdentityUser> signInManager)
    {
        _signInManager = signInManager;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await _signInManager.SignOutAsync();
        return Redirect("/admin/login");
    }
}
