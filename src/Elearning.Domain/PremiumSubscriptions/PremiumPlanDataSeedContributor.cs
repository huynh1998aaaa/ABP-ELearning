using System;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Uow;

namespace Elearning.PremiumSubscriptions;

public class PremiumPlanDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IGuidGenerator _guidGenerator;
    private readonly IRepository<PremiumPlan, Guid> _premiumPlanRepository;

    public PremiumPlanDataSeedContributor(
        IRepository<PremiumPlan, Guid> premiumPlanRepository,
        IGuidGenerator guidGenerator)
    {
        _premiumPlanRepository = premiumPlanRepository;
        _guidGenerator = guidGenerator;
    }

    [UnitOfWork]
    public virtual async Task SeedAsync(DataSeedContext context)
    {
        if (await _premiumPlanRepository.FindAsync(x => x.Code == PremiumPlanConsts.SixMonthsCode) != null)
        {
            return;
        }

        await _premiumPlanRepository.InsertAsync(new PremiumPlan(
            _guidGenerator.Create(),
            PremiumPlanConsts.SixMonthsCode,
            "Premium 6 tháng",
            PremiumPlanType.SixMonths,
            PremiumPlanConsts.SixMonthsDuration,
            price: 0,
            currency: "VND",
            sortOrder: 10,
            isSystem: true,
            description: "Gói Premium thủ công có hiệu lực trong 6 tháng từ ngày kích hoạt."));
    }
}
