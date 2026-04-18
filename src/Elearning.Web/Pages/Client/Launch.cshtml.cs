using System;
using System.Threading.Tasks;
using Elearning.ClientContent;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages.Client;

public class LaunchModel : ElearningClientPageModel
{
    private readonly IClientLearningSessionAppService _clientLearningSessionAppService;

    public LaunchModel(IClientLearningSessionAppService clientLearningSessionAppService)
    {
        _clientLearningSessionAppService = clientLearningSessionAppService;
    }

    [BindProperty(SupportsGet = true)]
    public ClientLearningItemKind Kind { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var launch = await _clientLearningSessionAppService.StartOrResumeAsync(Kind, Id);
        return RedirectToPage("/Client/Session", new { id = launch.SessionId });
    }
}
