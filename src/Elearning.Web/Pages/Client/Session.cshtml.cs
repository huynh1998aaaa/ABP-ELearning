using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elearning.ClientContent;
using Elearning.LearningSessions;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages.Client;

public class SessionModel : ElearningClientPageModel
{
    private readonly IClientLearningSessionAppService _clientLearningSessionAppService;

    public SessionModel(IClientLearningSessionAppService clientLearningSessionAppService)
    {
        _clientLearningSessionAppService = clientLearningSessionAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public ClientLearningSessionDto Session { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        Session = await _clientLearningSessionAppService.GetAsync(Id);

        if (Session.Status != LearningSessionStatus.InProgress)
        {
            return RedirectToPage("/Client/Result", new { id = Session.Id });
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSaveAnswerAsync(SaveClientLearningAnswerDto input)
    {
        try
        {
            await _clientLearningSessionAppService.SaveAnswerAsync(Id, input);

            return AjaxSuccess(new
            {
                questionId = input.QuestionId,
                isAnswered = input.SelectedOptionIds.Count > 0 ||
                             input.MatchingAnswers.Exists(x => !string.IsNullOrWhiteSpace(x.SelectedRightText)) ||
                             !string.IsNullOrWhiteSpace(input.EssayAnswerText)
            });
        }
        catch (Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }
    }

    public async Task<IActionResult> OnPostSubmitAsync()
    {
        try
        {
            var result = await _clientLearningSessionAppService.SubmitAsync(Id);
            return AjaxSuccess(new
            {
                redirectUrl = Url.Page("/Client/Result", new { id = result.Id })
            });
        }
        catch (Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }
    }
}
