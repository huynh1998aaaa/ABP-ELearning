using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Elearning.Permissions;
using Elearning.PremiumSubscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.Identity;

namespace Elearning.Web.Pages.Admin.PremiumSubscriptions;

[Authorize(ElearningPermissions.PremiumSubscriptions.Create)]
public class CreateModel : ElearningAdminPageModel
{
    private const int MaxIdentityQueryResultCount = 1000;

    private readonly IIdentityUserAppService _identityUserAppService;
    private readonly IPremiumPlanAppService _premiumPlanAppService;
    private readonly IUserPremiumSubscriptionAppService _subscriptionAppService;

    public CreateModel(
        IUserPremiumSubscriptionAppService subscriptionAppService,
        IPremiumPlanAppService premiumPlanAppService,
        IIdentityUserAppService identityUserAppService)
    {
        _subscriptionAppService = subscriptionAppService;
        _premiumPlanAppService = premiumPlanAppService;
        _identityUserAppService = identityUserAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid? UserId { get; set; }

    [BindProperty]
    public CreatePremiumSubscriptionInputModel Input { get; set; } = new();

    public List<SelectListItem> UserOptions { get; private set; } = new();

    public List<SelectListItem> PlanOptions { get; private set; } = new();

    public PremiumPlanDto? DefaultPlan { get; private set; }

    public async Task OnGetAsync()
    {
        await LoadOptionsAsync();

        if (UserId.HasValue)
        {
            Input.UserId = UserId.Value;
        }

        if (DefaultPlan != null)
        {
            Input.PremiumPlanId = DefaultPlan.Id;
        }
    }

    public async Task<IActionResult> OnGetModalAsync()
    {
        await OnGetAsync();
        return Partial("_CreateForm", this);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        await _subscriptionAppService.CreateAsync(new CreateUserPremiumSubscriptionDto
        {
            UserId = Input.UserId!.Value,
            PremiumPlanId = Input.PremiumPlanId!.Value,
            Note = Input.Note
        });

        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostModalAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            Response.StatusCode = 400;
            return Partial("_CreateForm", this);
        }

        try
        {
            await _subscriptionAppService.CreateAsync(new CreateUserPremiumSubscriptionDto
            {
                UserId = Input.UserId!.Value,
                PremiumPlanId = Input.PremiumPlanId!.Value,
                Note = Input.Note
            });

            return AjaxSuccess();
        }
        catch (System.Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }
    }

    private async Task LoadOptionsAsync()
    {
        DefaultPlan = await _premiumPlanAppService.GetDefaultSixMonthsPlanAsync();

        var plans = await _premiumPlanAppService.GetListAsync(new GetPremiumPlanListInput
        {
            IsActive = true,
            MaxResultCount = 1000,
            SkipCount = 0
        });

        PlanOptions = plans.Items
            .Select(x => new SelectListItem($"{x.DisplayName} ({x.DurationMonths} tháng)", x.Id.ToString()))
            .ToList();

        var users = await _identityUserAppService.GetListAsync(new GetIdentityUsersInput
        {
            MaxResultCount = MaxIdentityQueryResultCount,
            SkipCount = 0
        });

        UserOptions = users.Items
            .OrderBy(x => x.UserName)
            .Select(x => new SelectListItem($"{x.UserName} - {x.Email}", x.Id.ToString()))
            .ToList();
    }

    public class CreatePremiumSubscriptionInputModel
    {
        [Required]
        public Guid? UserId { get; set; }

        [Required]
        public Guid? PremiumPlanId { get; set; }

        [StringLength(PremiumSubscriptionConsts.MaxNoteLength)]
        public string? Note { get; set; }
    }
}
