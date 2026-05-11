using System;
using Microsoft.Extensions.Configuration;

namespace Elearning.Web.Security;

public static class GoogleAuthenticationSettings
{
    public static string? GetClientId(IConfiguration configuration)
    {
        return GetSetting(
            configuration,
            "Authentication:Google:ClientId",
            "Authentication__Google__ClientId",
            "GOOGLE_AUTH_CLIENT_ID");
    }

    public static string? GetClientSecret(IConfiguration configuration)
    {
        return GetSetting(
            configuration,
            "Authentication:Google:ClientSecret",
            "Authentication__Google__ClientSecret",
            "GOOGLE_AUTH_CLIENT_SECRET");
    }

    private static string? GetSetting(
        IConfiguration configuration,
        string configurationKey,
        string environmentKey,
        string aliasEnvironmentKey)
    {
        return Environment.GetEnvironmentVariable(environmentKey)
            ?? Environment.GetEnvironmentVariable(aliasEnvironmentKey)
            ?? configuration[configurationKey];
    }
}
