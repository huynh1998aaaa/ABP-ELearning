using Elearning.Samples;
using Xunit;

namespace Elearning.EntityFrameworkCore.Domains;

[Collection(ElearningTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<ElearningEntityFrameworkCoreTestModule>
{

}
