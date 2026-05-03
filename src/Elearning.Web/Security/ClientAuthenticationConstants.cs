using System.Security.Claims;
using System.Security.Principal;

namespace Elearning.Web.Security;

public static class ClientAuthenticationConstants
{
    public const string ClientAccessClaimType = "elearning_client_access";
    public const string LoginProviderClaimType = "elearning_client_login_provider";
    public const string GoogleLoginProvider = "Google";

    public static bool HasClientAccess(IPrincipal? principal)
    {
        return principal is ClaimsPrincipal claimsPrincipal &&
               string.Equals(
                   claimsPrincipal.FindFirstValue(ClientAccessClaimType),
                   bool.TrueString,
                   System.StringComparison.OrdinalIgnoreCase);
    }
}
