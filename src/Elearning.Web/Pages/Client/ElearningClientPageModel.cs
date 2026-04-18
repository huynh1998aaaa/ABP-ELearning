using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Elearning.Web.Pages;
using Volo.Abp;
using Volo.Abp.Validation;

namespace Elearning.Web.Pages.Client;

[Authorize]
public abstract class ElearningClientPageModel : ElearningPageModel
{
    protected bool IsAjaxRequest =>
        string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

    protected JsonResult AjaxSuccess(object? payload = null)
    {
        return new JsonResult(new { success = true, data = payload });
    }

    protected JsonResult AjaxError(Exception exception, int statusCode = 400)
    {
        return new JsonResult(new
        {
            error = new
            {
                message = GetFriendlyExceptionMessage(exception)
            }
        })
        {
            StatusCode = statusCode
        };
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
