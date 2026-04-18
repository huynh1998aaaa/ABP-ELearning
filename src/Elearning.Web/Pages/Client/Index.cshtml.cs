using System.Collections.Generic;
using System.Threading.Tasks;
using Elearning.ClientContent;

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

    public IReadOnlyList<ClientLearningItemDto> FreeItems { get; private set; } = new List<ClientLearningItemDto>();

    public IReadOnlyList<ClientLearningItemDto> PremiumItems { get; private set; } = new List<ClientLearningItemDto>();

    public async Task OnGetAsync()
    {
        var portal = await _clientLearningPortalAppService.GetAsync();
        IsPremium = portal.IsPremium;
        FreeItems = portal.FreeItems;
        PremiumItems = portal.PremiumItems;
    }
}
