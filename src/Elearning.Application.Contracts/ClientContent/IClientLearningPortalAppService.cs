using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Elearning.ClientContent;

public interface IClientLearningPortalAppService : IApplicationService
{
    Task<ClientLearningPortalDto> GetAsync();
}
