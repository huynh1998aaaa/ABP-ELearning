using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Elearning.PremiumSubscriptions;

public interface IPremiumPlanAppService : IApplicationService
{
    Task<PagedResultDto<PremiumPlanDto>> GetListAsync(GetPremiumPlanListInput input);

    Task<PremiumPlanDto> GetAsync(Guid id);

    Task<PremiumPlanDto> GetDefaultSixMonthsPlanAsync();

    Task<PremiumPlanDto> CreateAsync(CreatePremiumPlanDto input);

    Task<PremiumPlanDto> UpdateAsync(Guid id, UpdatePremiumPlanDto input);

    Task ActivateAsync(Guid id);

    Task DeactivateAsync(Guid id);
}
