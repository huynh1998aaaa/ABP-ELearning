using Volo.Abp.Modularity;

namespace Elearning;

[DependsOn(
    typeof(ElearningApplicationModule),
    typeof(ElearningDomainTestModule)
)]
public class ElearningApplicationTestModule : AbpModule
{

}
