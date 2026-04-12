using Volo.Abp.Modularity;

namespace Elearning;

[DependsOn(
    typeof(ElearningDomainModule),
    typeof(ElearningTestBaseModule)
)]
public class ElearningDomainTestModule : AbpModule
{

}
