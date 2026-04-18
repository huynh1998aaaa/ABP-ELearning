using System.Collections.Generic;

namespace Elearning.ClientContent;

public class ClientLearningPortalDto
{
    public bool IsPremium { get; set; }

    public List<ClientLearningItemDto> FreeItems { get; set; } = new();

    public List<ClientLearningItemDto> PremiumItems { get; set; } = new();
}
