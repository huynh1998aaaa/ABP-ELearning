using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Elearning.Permissions;
using Elearning.Web.Pages;
using Volo.Abp;
using Volo.Abp.Validation;

namespace Elearning.Web.Pages.Admin;

[Authorize(ElearningPermissions.AdminPortal.Access)]
public abstract class ElearningAdminPageModel : ElearningPageModel
{
    protected bool IsAjaxRequest =>
        string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", System.StringComparison.OrdinalIgnoreCase);

    protected JsonResult AjaxSuccess()
    {
        return new JsonResult(new { success = true });
    }

    protected JsonResult AjaxSuccess(string message)
    {
        return new JsonResult(new { success = true, message });
    }

    protected JsonResult AjaxError(string message, int statusCode = 400)
    {
        return new JsonResult(new { error = new { message } })
        {
            StatusCode = statusCode
        };
    }

    protected JsonResult AjaxError(Exception exception, int statusCode = 400)
    {
        return AjaxError(GetFriendlyExceptionMessage(exception), statusCode);
    }

    private string GetFriendlyExceptionMessage(Exception exception)
    {
        if (exception is BusinessException businessException && businessException.Data.Contains("Reason"))
        {
            return businessException.Data["Reason"]?.ToString() ?? businessException.Message;
        }

        if (exception is AbpValidationException validationException)
        {
            var message = validationException.ValidationErrors
                .Select(x => x.ErrorMessage)
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

            if (!string.IsNullOrWhiteSpace(message))
            {
                return message;
            }
        }

        return exception is UserFriendlyException
            ? exception.Message
            : L["Common:AjaxOperationFailed"];
    }
}
