using System.Linq;
using System.Threading.Tasks;
using Elearning.Permissions;
using Elearning.Questions;
using Elearning.QuestionTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages.Admin.Questions;

[Authorize(ElearningPermissions.Questions.Create)]
public class CreateModel : QuestionFormPageModel
{
    private readonly IQuestionAppService _questionAppService;

    public CreateModel(
        IQuestionAppService questionAppService,
        IQuestionTypeAppService questionTypeAppService)
        : base(questionTypeAppService)
    {
        _questionAppService = questionAppService;
    }

    [BindProperty]
    public CreateQuestionDto Input { get; set; } = new()
    {
        Difficulty = QuestionDifficulty.Medium,
        Score = 1,
        SortOrder = 100,
        IsActive = true
    };

    public async Task OnGetAsync()
    {
        await LoadQuestionTypesAsync(activeOnly: true);
        Input.QuestionTypeId = AvailableQuestionTypes.FirstOrDefault()?.Id ?? Input.QuestionTypeId;
        Input.Options = PadOptions(Input.Options);
        Input.MatchingPairs = PadMatchingPairs(Input.MatchingPairs);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadQuestionTypesAsync(activeOnly: true);
            Input.Options = PadOptions(Input.Options);
            Input.MatchingPairs = PadMatchingPairs(Input.MatchingPairs);
            return Page();
        }

        var question = await _questionAppService.CreateAsync(Input);
        return RedirectToPage("./Preview", new { id = question.Id });
    }
}
