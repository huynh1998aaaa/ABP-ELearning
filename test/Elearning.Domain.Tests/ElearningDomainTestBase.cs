using Volo.Abp.Modularity;

namespace Elearning;

/* Inherit from this class for your domain layer tests. */
public abstract class ElearningDomainTestBase<TStartupModule> : ElearningTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
