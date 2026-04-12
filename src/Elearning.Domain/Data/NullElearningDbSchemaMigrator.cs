using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Elearning.Data;

/* This is used if database provider does't define
 * IElearningDbSchemaMigrator implementation.
 */
public class NullElearningDbSchemaMigrator : IElearningDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
