using System;
using System.Threading.Tasks;
using Elearning.Permissions;
using Elearning.Questions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages.Admin.Questions;

[Authorize(ElearningPermissions.Questions.Default)]
public class PreviewModel : ElearningAdminPageModel
{
    private readonly IQuestionAppService _questionAppService;

    public PreviewModel(IQuestionAppService questionAppService)
    {
        _questionAppService = questionAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public QuestionDto Question { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        Question = await _questionAppService.GetAsync(Id);
        return Page();
    }
}
