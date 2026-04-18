using System;
using System.Threading.Tasks;
using Elearning.Permissions;
using Elearning.Subjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages.Admin.Subjects;

[Authorize(ElearningPermissions.Subjects.Update)]
public class EditModel : ElearningAdminPageModel
{
    private readonly ISubjectAppService _subjectAppService;

    public EditModel(ISubjectAppService subjectAppService)
    {
        _subjectAppService = subjectAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public UpdateSubjectDto Input { get; set; } = new();

    public bool IsActive { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadSubjectAsync();
        return Page();
    }

    public async Task<IActionResult> OnGetModalAsync()
    {
        await LoadSubjectAsync();
        return Partial("_EditForm", this);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadFlagsAsync();
            return Page();
        }

        await _subjectAppService.UpdateAsync(Id, Input);
        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostModalAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadFlagsAsync();
            Response.StatusCode = 400;
            return Partial("_EditForm", this);
        }

        try
        {
            await _subjectAppService.UpdateAsync(Id, Input);
            return AjaxSuccess();
        }
        catch (System.Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }
    }

    private async Task LoadSubjectAsync()
    {
        var subject = await _subjectAppService.GetAsync(Id);

        Input = new UpdateSubjectDto
        {
            Code = subject.Code,
            Name = subject.Name,
            Description = subject.Description,
            SortOrder = subject.SortOrder
        };

        IsActive = subject.IsActive;
    }

    private async Task LoadFlagsAsync()
    {
        var subject = await _subjectAppService.GetAsync(Id);
        IsActive = subject.IsActive;
    }
}
