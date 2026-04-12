using System.Threading.Tasks;

namespace Elearning.Data;

public interface IElearningDbSchemaMigrator
{
    Task MigrateAsync();
}
