using Elearning.Samples;
using Xunit;

namespace Elearning.EntityFrameworkCore.Applications;

[Collection(ElearningTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<ElearningEntityFrameworkCoreTestModule>
{

}
