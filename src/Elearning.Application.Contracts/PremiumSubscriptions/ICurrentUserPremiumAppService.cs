using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Elearning.PremiumSubscriptions;

public interface ICurrentUserPremiumAppService : IApplicationService
{
    Task<PremiumStatusDto> GetCurrentUserPremiumStatusAsync();
}
