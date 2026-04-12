using Elearning.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace Elearning.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(ElearningEntityFrameworkCoreModule),
    typeof(ElearningApplicationContractsModule)
    )]
public class ElearningDbMigratorModule : AbpModule
{
}
