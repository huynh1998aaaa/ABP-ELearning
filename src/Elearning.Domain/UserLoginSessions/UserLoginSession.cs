using System;
using Elearning.LoginSessions;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Elearning.UserLoginSessions;

public class UserLoginSession : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }

    public Guid UserId { get; private set; }

    public string DeviceId { get; private set; } = string.Empty;

    public string SessionKey { get; private set; } = string.Empty;

    public string Channel { get; private set; } = string.Empty;

    public string Provider { get; private set; } = string.Empty;

    public bool IsCurrent { get; private set; }

    public DateTime LastSeenAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    public string? RevokedReason { get; private set; }

    public string? ClientIp { get; private set; }

    public string? UserAgent { get; private set; }

    protected UserLoginSession()
    {
    }

    public UserLoginSession(
        Guid id,
        Guid? tenantId,
        Guid userId,
        string deviceId,
        string sessionKey,
        string channel,
        string provider,
        DateTime observedAt,
        string? clientIp = null,
        string? userAgent = null)
        : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        DeviceId = Check.NotNullOrWhiteSpace(deviceId, nameof(deviceId), UserLoginSessionConsts.MaxDeviceIdLength);
        SessionKey = Check.NotNullOrWhiteSpace(sessionKey, nameof(sessionKey), UserLoginSessionConsts.MaxSessionKeyLength);
        Channel = Check.NotNullOrWhiteSpace(channel, nameof(channel), UserLoginSessionConsts.MaxChannelLength);
        Provider = Check.NotNullOrWhiteSpace(provider, nameof(provider), UserLoginSessionConsts.MaxProviderLength);
        LastSeenAt = observedAt;
        ClientIp = Check.Length(clientIp, nameof(clientIp), UserLoginSessionConsts.MaxClientIpLength);
        UserAgent = Check.Length(userAgent, nameof(userAgent), UserLoginSessionConsts.MaxUserAgentLength);
        IsCurrent = true;
    }

    public void Refresh(
        string sessionKey,
        string channel,
        string provider,
        DateTime observedAt,
        string? clientIp = null,
        string? userAgent = null)
    {
        SessionKey = Check.NotNullOrWhiteSpace(sessionKey, nameof(sessionKey), UserLoginSessionConsts.MaxSessionKeyLength);
        Channel = Check.NotNullOrWhiteSpace(channel, nameof(channel), UserLoginSessionConsts.MaxChannelLength);
        Provider = Check.NotNullOrWhiteSpace(provider, nameof(provider), UserLoginSessionConsts.MaxProviderLength);
        LastSeenAt = observedAt;
        ClientIp = Check.Length(clientIp, nameof(clientIp), UserLoginSessionConsts.MaxClientIpLength);
        UserAgent = Check.Length(userAgent, nameof(userAgent), UserLoginSessionConsts.MaxUserAgentLength);
        IsCurrent = true;
        RevokedAt = null;
        RevokedReason = null;
    }

    public void Touch(
        DateTime observedAt,
        string? clientIp = null,
        string? userAgent = null)
    {
        LastSeenAt = observedAt;
        ClientIp = Check.Length(clientIp, nameof(clientIp), UserLoginSessionConsts.MaxClientIpLength);
        UserAgent = Check.Length(userAgent, nameof(userAgent), UserLoginSessionConsts.MaxUserAgentLength);
    }

    public void Revoke(DateTime revokedAt, string reason)
    {
        IsCurrent = false;
        RevokedAt = revokedAt;
        RevokedReason = Check.NotNullOrWhiteSpace(reason, nameof(reason), UserLoginSessionConsts.MaxRevokedReasonLength);
    }
}
