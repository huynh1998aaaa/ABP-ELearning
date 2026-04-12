using System;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages;

public class LoginModel : ElearningPageModel
{
    [BindProperty(SupportsGet = true)]
    public string ReturnUrl { get; set; } = "/client";

    public IActionResult OnGet()
    {
        return Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(ReturnUrl)}");
    }
}
