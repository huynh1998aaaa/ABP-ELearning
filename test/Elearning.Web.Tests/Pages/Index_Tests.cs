using System.Net;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Elearning.Pages;

public class Index_Tests : ElearningWebTestBase
{
    [Fact]
    public async Task Welcome_Page()
    {
        var response = await GetResponseAsync("/", HttpStatusCode.Found);

        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location!.OriginalString.ShouldBeOneOf("/admin", "/client");
    }
}
