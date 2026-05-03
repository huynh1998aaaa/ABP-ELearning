using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Elearning.Identity;
using Elearning.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using AbpIdentityUser = Volo.Abp.Identity.IdentityUser;
using AbpIdentityRole = Volo.Abp.Identity.IdentityRole;

namespace Elearning.Web.Pages.Client;

[AllowAnonymous]
public class GoogleCallbackModel : ElearningPageModel
{
    private readonly IGuidGenerator _guidGenerator;
    private readonly IdentityRoleManager _identityRoleManager;
    private readonly IdentityUserManager _identityUserManager;
    private readonly SignInManager<AbpIdentityUser> _signInManager;

    public GoogleCallbackModel(
        IdentityUserManager identityUserManager,
        IdentityRoleManager identityRoleManager,
        SignInManager<AbpIdentityUser> signInManager,
        IGuidGenerator guidGenerator)
    {
        _identityUserManager = identityUserManager;
        _identityRoleManager = identityRoleManager;
        _signInManager = signInManager;
        _guidGenerator = guidGenerator;
    }

    [BindProperty(SupportsGet = true)]
    public string ReturnUrl { get; set; } = "/client";

    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? remoteError = null)
    {
        ReturnUrl = GetSafeReturnUrl(ReturnUrl);

        if (!string.IsNullOrWhiteSpace(remoteError))
        {
            ErrorMessage = L["Auth:GoogleRemoteError", remoteError];
            return Page();
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            ErrorMessage = L["Auth:GoogleExternalInfoMissing"];
            return Page();
        }

        var signInResult = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider,
            info.ProviderKey,
            isPersistent: true,
            bypassTwoFactor: true);

        if (signInResult.Succeeded)
        {
            var existingUser = await _identityUserManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (existingUser == null || !existingUser.IsActive)
            {
                ErrorMessage = L["Auth:GoogleAccountCannotSignIn"];
                await _signInManager.SignOutAsync();
                return Page();
            }

            await SignInClientUserAsync(existingUser, info.LoginProvider);
            return LocalRedirect(ReturnUrl);
        }

        if (signInResult.IsLockedOut || signInResult.IsNotAllowed)
        {
            ErrorMessage = L["Auth:GoogleAccountCannotSignIn"];
            return Page();
        }

        var user = await FindOrCreateUserAsync(info);
        if (user == null)
        {
            return Page();
        }

        if (!user.IsActive)
        {
            ErrorMessage = L["Auth:GoogleAccountCannotSignIn"];
            return Page();
        }

        var addLoginResult = await _identityUserManager.AddLoginAsync(user, info);
        if (!addLoginResult.Succeeded)
        {
            AddIdentityErrors(addLoginResult);
            return Page();
        }

        if (!await EnsureDefaultUserRoleAsync(user))
        {
            return Page();
        }

        await SignInClientUserAsync(user, info.LoginProvider);

        return LocalRedirect(ReturnUrl);
    }

    private async Task SignInClientUserAsync(AbpIdentityUser user, string loginProvider)
    {
        await _signInManager.SignInWithClaimsAsync(
            user,
            isPersistent: true,
            new[]
            {
                new Claim(ClientAuthenticationConstants.ClientAccessClaimType, bool.TrueString),
                new Claim(ClientAuthenticationConstants.LoginProviderClaimType, loginProvider)
            });
    }

    private async Task<AbpIdentityUser?> FindOrCreateUserAsync(ExternalLoginInfo info)
    {
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            ErrorMessage = L["Auth:GoogleEmailMissing"];
            return null;
        }

        if (!IsVerifiedGoogleEmail(info))
        {
            ErrorMessage = L["Auth:GoogleEmailNotVerified"];
            return null;
        }

        var user = await _identityUserManager.FindByEmailAsync(email);
        if (user != null)
        {
            if (!user.EmailConfirmed)
            {
                user.SetEmailConfirmed(true);
                var updateResult = await _identityUserManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    AddIdentityErrors(updateResult);
                    return null;
                }
            }

            return user;
        }

        user = new AbpIdentityUser(_guidGenerator.Create(), email, email, CurrentTenant.Id);
        user.SetEmailConfirmed(true);

        var createResult = await _identityUserManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            AddIdentityErrors(createResult);
            return null;
        }

        return user;
    }

    private async Task<bool> EnsureDefaultUserRoleAsync(AbpIdentityUser user)
    {
        var role = await _identityRoleManager.FindByNameAsync(ElearningRoleNames.User);
        if (role == null)
        {
            var createRoleResult = await _identityRoleManager.CreateAsync(
                new AbpIdentityRole(_guidGenerator.Create(), ElearningRoleNames.User, CurrentTenant.Id));
            if (!createRoleResult.Succeeded)
            {
                AddIdentityErrors(createRoleResult);
                return false;
            }
        }

        if (await _identityUserManager.IsInRoleAsync(user, ElearningRoleNames.User))
        {
            return true;
        }

        var addRoleResult = await _identityUserManager.AddToRoleAsync(user, ElearningRoleNames.User);
        if (!addRoleResult.Succeeded)
        {
            AddIdentityErrors(addRoleResult);
            return false;
        }

        return true;
    }

    private static bool IsVerifiedGoogleEmail(ExternalLoginInfo info)
    {
        var value = info.Principal.Claims.FirstOrDefault(x => x.Type == "email_verified")?.Value;
        return bool.TryParse(value, out var verified) && verified;
    }

    private string GetSafeReturnUrl(string? returnUrl)
    {
        return Url.IsLocalUrl(returnUrl) &&
               returnUrl!.StartsWith("/client", StringComparison.OrdinalIgnoreCase)
            ? returnUrl
            : "/client";
    }

    private void AddIdentityErrors(IdentityResult identityResult)
    {
        var errors = identityResult.Errors.Select(x => x.Description).ToList();
        ErrorMessage = errors.FirstOrDefault() ?? L["Auth:GoogleLoginFailed"];
        foreach (var error in errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }
    }
}
