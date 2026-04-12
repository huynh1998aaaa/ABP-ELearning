using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Elearning.PremiumSubscriptions;

public interface IUserPremiumSubscriptionAppService : IApplicationService
{
    Task<PagedResultDto<UserPremiumSubscriptionDto>> GetListAsync(GetUserPremiumSubscriptionListInput input);

    Task<UserPremiumSubscriptionDto> GetAsync(Guid id);

    Task<UserPremiumSubscriptionDto?> GetActiveByUserAsync(Guid userId);

    Task<PremiumStatusDto> GetUserPremiumStatusAsync(Guid userId);

    Task<UserPremiumSubscriptionDto> CreateAsync(CreateUserPremiumSubscriptionDto input);

    Task<UserPremiumSubscriptionDto> ExtendAsync(Guid id);

    Task CancelAsync(Guid id, CancelPremiumSubscriptionDto input);
}
