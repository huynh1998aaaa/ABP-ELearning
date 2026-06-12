using System.Net;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Elearning.Pages;

public class LoginFlow_Tests : ElearningWebTestBase
{
    [Fact]
    public async Task Account_Login_Should_Redirect_To_Admin_Login()
    {
        var response = await GetResponseAsync("/Account/Login", HttpStatusCode.Found);

        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location!.OriginalString.ShouldStartWith("/admin/login?returnUrl=");
    }

    [Fact]
    public async Task Account_Logout_Should_Redirect_To_Admin()
    {
        var response = await GetResponseAsync("/Account/Logout", HttpStatusCode.Found);

        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location!.OriginalString.ShouldBe("/admin");
    }
}
