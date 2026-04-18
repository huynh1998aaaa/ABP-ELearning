using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.Uow;

namespace Elearning.Identity;

public class DefaultRoleDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IGuidGenerator _guidGenerator;
    private readonly IdentityRoleManager _roleManager;

    public DefaultRoleDataSeedContributor(
        IGuidGenerator guidGenerator,
        IdentityRoleManager roleManager)
    {
        _guidGenerator = guidGenerator;
        _roleManager = roleManager;
    }

    [UnitOfWork]
    public virtual async Task SeedAsync(DataSeedContext context)
    {
        if (await _roleManager.FindByNameAsync(ElearningRoleNames.User) != null)
        {
            return;
        }

        var result = await _roleManager.CreateAsync(
            new IdentityRole(_guidGenerator.Create(), ElearningRoleNames.User, context.TenantId));

        if (!result.Succeeded)
        {
            throw new AbpException(
                "Could not create default user role: " +
                string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }
}
