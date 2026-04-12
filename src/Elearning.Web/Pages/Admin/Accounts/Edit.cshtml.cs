using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.Identity;

namespace Elearning.Web.Pages.Admin.Accounts;

[Authorize(IdentityPermissions.Users.Update)]
public class EditModel : ElearningAdminPageModel
{
    private const int MaxIdentityQueryResultCount = 1000;

    private readonly IIdentityUserRepository _identityUserRepository;
    private readonly IdentityUserManager _identityUserManager;
    private readonly IIdentityRoleAppService _identityRoleAppService;
    private readonly IIdentityUserAppService _identityUserAppService;

    public EditModel(
        IIdentityUserAppService identityUserAppService,
        IIdentityRoleAppService identityRoleAppService,
        IIdentityUserRepository identityUserRepository,
        IdentityUserManager identityUserManager)
    {
        _identityUserAppService = identityUserAppService;
        _identityRoleAppService = identityRoleAppService;
        _identityUserRepository = identityUserRepository;
        _identityUserManager = identityUserManager;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public EditAccountInputModel Input { get; set; } = new();

    [BindProperty]
    public ResetPasswordInputModel ResetPassword { get; set; } = new();

    public List<SelectListItem> AvailableRoles { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadRolesAsync();
        await LoadUserAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Remove($"{nameof(ResetPassword)}.{nameof(ResetPassword.NewPassword)}");
        ModelState.Remove($"{nameof(ResetPassword)}.{nameof(ResetPassword.ConfirmPassword)}");

        if (!ModelState.IsValid)
        {
            await LoadRolesAsync();
            await LoadUserAsync();
            return Page();
        }

        await _identityUserAppService.UpdateAsync(Id, new IdentityUserUpdateDto
        {
            UserName = Input.UserName,
            Email = Input.Email,
            Name = Input.Name,
            Surname = Input.Surname,
            PhoneNumber = Input.PhoneNumber,
            IsActive = Input.IsActive,
            ConcurrencyStamp = Input.ConcurrencyStamp
        });

        await _identityUserAppService.UpdateRolesAsync(Id, new IdentityUserUpdateRolesDto
        {
            RoleNames = Input.RoleNames.ToArray()
        });

        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostResetPasswordAsync()
    {
        ModelState.Remove($"{nameof(Input)}.{nameof(Input.UserName)}");
        ModelState.Remove($"{nameof(Input)}.{nameof(Input.Email)}");
        ModelState.Remove($"{nameof(Input)}.{nameof(Input.Name)}");
        ModelState.Remove($"{nameof(Input)}.{nameof(Input.Surname)}");
        ModelState.Remove($"{nameof(Input)}.{nameof(Input.PhoneNumber)}");
        ModelState.Remove($"{nameof(Input)}.{nameof(Input.RoleNames)}");
        ModelState.Remove($"{nameof(Input)}.{nameof(Input.IsActive)}");
        ModelState.Remove($"{nameof(Input)}.{nameof(Input.ConcurrencyStamp)}");

        if (!ModelState.IsValid)
        {
            await LoadRolesAsync();
            await LoadUserAsync();
            return Page();
        }

        var user = await _identityUserRepository.GetAsync(Id);

        if (await _identityUserManager.HasPasswordAsync(user))
        {
            var removeResult = await _identityUserManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
            {
                AddIdentityErrors(removeResult);
                await LoadRolesAsync();
                await LoadUserAsync();
                return Page();
            }
        }

        var addPasswordResult = await _identityUserManager.AddPasswordAsync(user, ResetPassword.NewPassword);
        if (!addPasswordResult.Succeeded)
        {
            AddIdentityErrors(addPasswordResult);
            await LoadRolesAsync();
            await LoadUserAsync();
            return Page();
        }

        return RedirectToPage(new { id = Id });
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

    private async Task LoadUserAsync()
    {
        var user = await _identityUserAppService.GetAsync(Id);
        var roles = await _identityUserAppService.GetRolesAsync(Id);

        Input = new EditAccountInputModel
        {
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Name = user.Name,
            Surname = user.Surname,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            ConcurrencyStamp = user.ConcurrencyStamp,
            RoleNames = roles.Items.Select(x => x.Name).ToList()
        };
    }

    private void AddIdentityErrors(IdentityResult identityResult)
    {
        foreach (var error in identityResult.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    public class EditAccountInputModel
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

        public List<string> RoleNames { get; set; } = new();

        public bool IsActive { get; set; } = true;

        public string? ConcurrencyStamp { get; set; }
    }

    public class ResetPasswordInputModel
    {
        [Required]
        [DataType(DataType.Password)]
        [StringLength(128, MinimumLength = 6)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword))]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
