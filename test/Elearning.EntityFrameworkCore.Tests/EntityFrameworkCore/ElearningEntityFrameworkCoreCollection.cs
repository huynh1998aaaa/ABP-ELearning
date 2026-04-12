using Xunit;

namespace Elearning.EntityFrameworkCore;

[CollectionDefinition(ElearningTestConsts.CollectionDefinitionName)]
public class ElearningEntityFrameworkCoreCollection : ICollectionFixture<ElearningEntityFrameworkCoreFixture>
{

}
