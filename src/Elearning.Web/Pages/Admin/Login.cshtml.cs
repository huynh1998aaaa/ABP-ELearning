using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Elearning.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Volo.Abp.Identity;
using AbpIdentityUser = Volo.Abp.Identity.IdentityUser;

namespace Elearning.Web.Pages.Admin;

[AllowAnonymous]
public class LoginModel : ElearningPageModel
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IdentityUserManager _identityUserManager;
    private readonly SignInManager<AbpIdentityUser> _signInManager;

    public LoginModel(
        IdentityUserManager identityUserManager,
        SignInManager<AbpIdentityUser> signInManager,
        IAuthorizationService authorizationService)
    {
        _identityUserManager = identityUserManager;
        _signInManager = signInManager;
        _authorizationService = authorizationService;
    }

    [BindProperty(SupportsGet = true)]
    public string ReturnUrl { get; set; } = "/admin";

    [BindProperty(SupportsGet = true)]
    public bool AccessDenied { get; set; }

    [BindProperty]
    public AdminLoginInputModel Input { get; set; } = new();

    [TempData]
    public string? FlashErrorMessage { get; set; }

    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        ReturnUrl = GetSafeReturnUrl(ReturnUrl);

        ErrorMessage = FlashErrorMessage;
        FlashErrorMessage = null;

        if (AccessDenied)
        {
            ErrorMessage = L["Auth:AdminAccessDenied"];
        }

        if (!CurrentUser.IsAuthenticated)
        {
            return Page();
        }

        var hasAdminAccess = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.AdminPortal.Access)).Succeeded;
        if (hasAdminAccess)
        {
            return LocalRedirect(ReturnUrl);
        }

        await _signInManager.SignOutAsync();
        return LocalRedirect(BuildLoginUrl(returnUrl: ReturnUrl, accessDenied: true));
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ReturnUrl = GetSafeReturnUrl(ReturnUrl);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await FindUserAsync(Input.UserNameOrEmailAddress);
        if (user == null)
        {
            AddInvalidLoginError();
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName,
            Input.Password,
            Input.RememberMe,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            AddInvalidLoginError();
            return Page();
        }

        var principal = await _signInManager.CreateUserPrincipalAsync(user);
        var hasAdminAccess = (await _authorizationService.AuthorizeAsync(principal, ElearningPermissions.AdminPortal.Access)).Succeeded;
        if (!hasAdminAccess)
        {
            await _signInManager.SignOutAsync();
            FlashErrorMessage = L["Auth:AdminAccessDenied"];
            return LocalRedirect(BuildLoginUrl(returnUrl: ReturnUrl, accessDenied: true));
        }

        return LocalRedirect(ReturnUrl);
    }

    private async Task<AbpIdentityUser?> FindUserAsync(string userNameOrEmailAddress)
    {
        var value = userNameOrEmailAddress.Trim();
        return value.Contains('@', StringComparison.Ordinal)
            ? await _identityUserManager.FindByEmailAsync(value)
            : await _identityUserManager.FindByNameAsync(value);
    }

    private string GetSafeReturnUrl(string? returnUrl)
    {
        return Url.IsLocalUrl(returnUrl) &&
               returnUrl!.StartsWith("/admin", StringComparison.OrdinalIgnoreCase)
            ? returnUrl
            : "/admin";
    }

    private string BuildLoginUrl(string returnUrl, bool accessDenied = false)
    {
        var loginUrl = $"/admin/login?returnUrl={Uri.EscapeDataString(returnUrl)}";
        return accessDenied ? $"{loginUrl}&accessDenied=true" : loginUrl;
    }

    private void AddInvalidLoginError()
    {
        ModelState.AddModelError(string.Empty, L["Auth:InvalidAdminLogin"]);
    }

    public class AdminLoginInputModel
    {
        [Required]
        [StringLength(256)]
        public string UserNameOrEmailAddress { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = true;
    }
}
