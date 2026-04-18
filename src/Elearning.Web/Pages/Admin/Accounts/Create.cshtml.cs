using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elearning.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.Identity;

namespace Elearning.Web.Pages.Admin.Accounts;

[Authorize(IdentityPermissions.Users.Create)]
public class CreateModel : ElearningAdminPageModel
{
    private const int MaxIdentityQueryResultCount = 1000;

    private readonly IIdentityRoleAppService _identityRoleAppService;
    private readonly IIdentityUserAppService _identityUserAppService;

    public CreateModel(
        IIdentityUserAppService identityUserAppService,
        IIdentityRoleAppService identityRoleAppService)
    {
        _identityUserAppService = identityUserAppService;
        _identityRoleAppService = identityRoleAppService;
    }

    [BindProperty]
    public CreateAccountInputModel Input { get; set; } = new();

    public List<SelectListItem> AvailableRoles { get; private set; } = new();

    public async Task OnGetAsync()
    {
        await LoadRolesAsync();
        EnsureDefaultUserRoleSelected();
    }

    public async Task<IActionResult> OnGetModalAsync()
    {
        await LoadRolesAsync();
        EnsureDefaultUserRoleSelected();
        return Partial("_CreateForm", this);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadRolesAsync();
        EnsureDefaultUserRoleSelected();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _identityUserAppService.CreateAsync(new IdentityUserCreateDto
        {
            UserName = Input.UserName,
            Email = Input.Email,
            Name = Input.Name,
            Surname = Input.Surname,
            PhoneNumber = Input.PhoneNumber,
            Password = Input.Password,
            IsActive = Input.IsActive,
            RoleNames = Input.RoleNames.ToArray()
        });

        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostModalAsync()
    {
        await LoadRolesAsync();
        EnsureDefaultUserRoleSelected();

        if (!ModelState.IsValid)
        {
            Response.StatusCode = 400;
            return Partial("_CreateForm", this);
        }

        try
        {
            await _identityUserAppService.CreateAsync(new IdentityUserCreateDto
            {
                UserName = Input.UserName,
                Email = Input.Email,
                Name = Input.Name,
                Surname = Input.Surname,
                PhoneNumber = Input.PhoneNumber,
                Password = Input.Password,
                IsActive = Input.IsActive,
                RoleNames = Input.RoleNames.ToArray()
            });

            return AjaxSuccess();
        }
        catch (System.Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }
    }

    private async Task LoadRolesAsync()
    {
        var roles = await _identityRoleAppService.GetListAsync(new GetIdentityRolesInput
        {
            MaxResultCount = MaxIdentityQueryResultCount,
            SkipCount = 0
        });

        AvailableRoles = roles.Items
            .Select(x => new SelectListItem(x.Name, x.Name))
            .ToList();
    }

    private void EnsureDefaultUserRoleSelected()
    {
        if (Input.RoleNames.Count > 0 || AvailableRoles.All(role => role.Value != ElearningRoleNames.User))
        {
            return;
        }

        Input.RoleNames.Add(ElearningRoleNames.User);
    }

    public class CreateAccountInputModel
    {
        [Required]
        [StringLength(256)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [StringLength(64)]
        public string? Name { get; set; }

        [StringLength(64)]
        public string? Surname { get; set; }

        [Phone]
        [StringLength(32)]
        public string? PhoneNumber { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [StringLength(128, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        public List<string> RoleNames { get; set; } = new();

        public bool IsActive { get; set; } = true;
    }
}
