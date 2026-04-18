using System;
using System.Threading.Tasks;
using Elearning.ClientContent;
using Elearning.LearningSessions;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages.Client;

public class ResultModel : ElearningClientPageModel
{
    private readonly IClientLearningSessionAppService _clientLearningSessionAppService;

    public ResultModel(IClientLearningSessionAppService clientLearningSessionAppService)
    {
        _clientLearningSessionAppService = clientLearningSessionAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public ClientLearningSessionResultDto Result { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var session = await _clientLearningSessionAppService.GetAsync(Id);
        if (session.Status == LearningSessionStatus.InProgress)
        {
            return RedirectToPage("/Client/Session", new { id = session.Id });
        }

        Result = await _clientLearningSessionAppService.GetResultAsync(Id);
        return Page();
    }
}
