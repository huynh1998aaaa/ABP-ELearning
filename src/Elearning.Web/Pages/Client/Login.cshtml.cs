using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using AbpIdentityUser = Volo.Abp.Identity.IdentityUser;

namespace Elearning.Web.Pages.Client;

[AllowAnonymous]
public class LoginModel : ElearningPageModel
{
    private readonly IConfiguration _configuration;
    private readonly SignInManager<AbpIdentityUser> _signInManager;

    public LoginModel(
        IConfiguration configuration,
        SignInManager<AbpIdentityUser> signInManager)
    {
        _configuration = configuration;
        _signInManager = signInManager;
    }

    [BindProperty(SupportsGet = true)]
    public string ReturnUrl { get; set; } = "/client";

    [BindProperty(SupportsGet = true)]
    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        ReturnUrl = GetSafeReturnUrl(ReturnUrl);

        if (CurrentUser.IsAuthenticated)
        {
            return LocalRedirect(ReturnUrl);
        }

        if (!string.IsNullOrWhiteSpace(ErrorMessage))
        {
            return Page();
        }

        if (!IsGoogleConfigured())
        {
            ErrorMessage = L["Auth:GoogleNotConfigured"];
            return Page();
        }

        var redirectUrl = $"/client/google-callback?returnUrl={Uri.EscapeDataString(ReturnUrl)}";
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(
            GoogleDefaults.AuthenticationScheme,
            redirectUrl);

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    private bool IsGoogleConfigured()
    {
        return !string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientId"]) &&
               !string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientSecret"]);
    }

    private string GetSafeReturnUrl(string? returnUrl)
    {
        return Url.IsLocalUrl(returnUrl) &&
               returnUrl!.StartsWith("/client", StringComparison.OrdinalIgnoreCase)
            ? returnUrl
            : "/client";
    }
}
