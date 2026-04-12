using System;
using System.Linq;
using System.Threading.Tasks;
using Elearning.Permissions;
using Elearning.Questions;
using Elearning.QuestionTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages.Admin.Questions;

[Authorize(ElearningPermissions.Questions.Update)]
public class EditModel : QuestionFormPageModel
{
    private readonly IQuestionAppService _questionAppService;

    public EditModel(
        IQuestionAppService questionAppService,
        IQuestionTypeAppService questionTypeAppService)
        : base(questionTypeAppService)
    {
        _questionAppService = questionAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public UpdateQuestionDto Input { get; set; } = new();

    public QuestionDto Question { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadQuestionTypesAsync(activeOnly: false);
        await LoadQuestionAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadQuestionTypesAsync(activeOnly: false);
            Question = await _questionAppService.GetAsync(Id);
            Input.Options = PadOptions(Input.Options);
            Input.MatchingPairs = PadMatchingPairs(Input.MatchingPairs);
            return Page();
        }

        await _questionAppService.UpdateAsync(Id, Input);
        return RedirectToPage("./Preview", new { id = Id });
    }

    private async Task LoadQuestionAsync()
    {
        Question = await _questionAppService.GetAsync(Id);
        Input = new UpdateQuestionDto
        {
            QuestionTypeId = Question.QuestionTypeId,
            Title = Question.Title,
            Content = Question.Content,
            Explanation = Question.Explanation,
            Difficulty = Question.Difficulty,
            Score = Question.Score,
            SortOrder = Question.SortOrder,
            Options = PadOptions(Question.Options.Select(x => new QuestionOptionInputDto
            {
                Text = x.Text,
                IsCorrect = x.IsCorrect,
                SortOrder = x.SortOrder
            })),
            MatchingPairs = PadMatchingPairs(Question.MatchingPairs.Select(x => new QuestionMatchingPairInputDto
            {
                LeftText = x.LeftText,
                RightText = x.RightText,
                SortOrder = x.SortOrder
            })),
            EssayAnswer = new QuestionEssayAnswerInputDto
            {
                SampleAnswer = Question.EssayAnswer?.SampleAnswer,
                Rubric = Question.EssayAnswer?.Rubric,
                MaxWords = Question.EssayAnswer?.MaxWords
            }
        };
    }
}
