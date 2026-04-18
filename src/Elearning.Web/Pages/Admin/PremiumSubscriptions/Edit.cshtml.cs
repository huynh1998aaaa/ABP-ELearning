using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Elearning.Permissions;
using Elearning.PremiumSubscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages.Admin.PremiumSubscriptions;

[Authorize(ElearningPermissions.PremiumSubscriptions.Default)]
public class EditModel : ElearningAdminPageModel
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IUserPremiumSubscriptionAppService _subscriptionAppService;

    public EditModel(
        IUserPremiumSubscriptionAppService subscriptionAppService,
        IAuthorizationService authorizationService)
    {
        _subscriptionAppService = subscriptionAppService;
        _authorizationService = authorizationService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CancelPremiumInputModel CancelInput { get; set; } = new();

    public UserPremiumSubscriptionDto Subscription { get; private set; } = new();

    public bool CanUpdate { get; private set; }

    public bool CanCancel { get; private set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnGetModalAsync()
    {
        await LoadAsync();
        return Partial("_EditForm", this);
    }

    public async Task<IActionResult> OnPostExtendAsync()
    {
        try
        {
            await _subscriptionAppService.ExtendAsync(Id);
            if (IsAjaxRequest)
            {
                return AjaxSuccess();
            }
        }
        catch (Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostCancelAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            if (IsAjaxRequest)
            {
                Response.StatusCode = 400;
                return Partial("_EditForm", this);
            }

            return Page();
        }

        try
        {
            await _subscriptionAppService.CancelAsync(Id, new CancelPremiumSubscriptionDto
            {
                Reason = CancelInput.Reason
            });

            if (IsAjaxRequest)
            {
                return AjaxSuccess();
            }
        }
        catch (Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }

        return RedirectToPage(new { id = Id });
    }

    private async Task LoadAsync()
    {
        CanUpdate = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.PremiumSubscriptions.Update)).Succeeded;
        CanCancel = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.PremiumSubscriptions.Cancel)).Succeeded;
        Subscription = await _subscriptionAppService.GetAsync(Id);
    }

    public class CancelPremiumInputModel
    {
        [StringLength(PremiumSubscriptionConsts.MaxCancellationReasonLength)]
        public string? Reason { get; set; }
    }
}
