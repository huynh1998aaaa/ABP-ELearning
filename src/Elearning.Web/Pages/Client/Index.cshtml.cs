using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elearning.ClientContent;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages.Client;

public class IndexModel : ElearningClientPageModel
{
    private readonly IClientLearningPortalAppService _clientLearningPortalAppService;

    public IndexModel(IClientLearningPortalAppService clientLearningPortalAppService)
    {
        _clientLearningPortalAppService = clientLearningPortalAppService;
    }

    public string? UserName => CurrentUser.UserName;

    public bool IsPremium { get; private set; }

    public DateTime? PremiumEndTime { get; private set; }

    [TempData]
    public string? ClientErrorMessage { get; set; }

    public IReadOnlyList<ClientLearningItemDto> FreeItems { get; private set; } = new List<ClientLearningItemDto>();

    public IReadOnlyList<ClientLearningItemDto> PremiumItems { get; private set; } = new List<ClientLearningItemDto>();

    public async Task OnGetAsync()
    {
        var portal = await _clientLearningPortalAppService.GetAsync();
        IsPremium = portal.IsPremium;
        PremiumEndTime = portal.PremiumEndTime;
        FreeItems = portal.FreeItems;
        PremiumItems = portal.PremiumItems;
    }
}
