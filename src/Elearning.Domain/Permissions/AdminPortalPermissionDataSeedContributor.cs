using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Uow;

namespace Elearning.Permissions;

public class AdminPortalPermissionDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private const string AdminPortalAccessPermissionName = "Elearning.AdminPortal";
    private const string RolePermissionProviderName = "R";

    private readonly IPermissionDataSeeder _permissionDataSeeder;

    public AdminPortalPermissionDataSeedContributor(IPermissionDataSeeder permissionDataSeeder)
    {
        _permissionDataSeeder = permissionDataSeeder;
    }

    [UnitOfWork]
    public virtual async Task SeedAsync(DataSeedContext context)
    {
        await _permissionDataSeeder.SeedAsync(
            RolePermissionProviderName,
            "admin",
            new[] { AdminPortalAccessPermissionName },
            context.TenantId);
    }
}
