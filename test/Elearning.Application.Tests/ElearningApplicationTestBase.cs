using Volo.Abp.Modularity;

namespace Elearning;

public abstract class ElearningApplicationTestBase<TStartupModule> : ElearningTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
