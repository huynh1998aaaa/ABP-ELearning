using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Elearning.Pages;

public class Index_Tests : ElearningWebTestBase
{
    [Fact]
    public async Task Welcome_Page()
    {
        var response = await GetResponseAsStringAsync("/");
        response.ShouldNotBeNull();
    }
}
