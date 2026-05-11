using System;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages.Account;

public class LoginModel : ElearningPageModel
{
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public IActionResult OnGet()
    {
        var returnUrl = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/admin";
        return Redirect($"/admin/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
    }
}
