namespace Elearning.LoginSessions;

public static class UserLoginSessionConsts
{
    public const int MaxDeviceIdLength = 64;
    public const int MaxSessionKeyLength = 128;
    public const int MaxChannelLength = 32;
    public const int MaxProviderLength = 32;
    public const int MaxRevokedReasonLength = 64;
    public const int MaxClientIpLength = 64;
    public const int MaxUserAgentLength = 512;

    public const string ChannelAdmin = "Admin";
    public const string ChannelClient = "Client";

    public const string ProviderPassword = "Password";
    public const string ProviderGoogle = "Google";

    public const string RevokedBecauseReplacedByAnotherDevice = "ReplacedByAnotherDevice";
    public const string RevokedBecauseLoggedOut = "LoggedOut";
}
