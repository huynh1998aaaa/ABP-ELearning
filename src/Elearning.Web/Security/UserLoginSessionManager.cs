using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Elearning.LoginSessions;
using Elearning.UserLoginSessions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Linq;
using Volo.Abp.Security.Claims;
using Volo.Abp.Timing;
using AbpIdentityUser = Volo.Abp.Identity.IdentityUser;

namespace Elearning.Web.Security;

public class UserLoginSessionManager : ITransientDependency
{
    private static readonly TimeSpan LastSeenThrottle = TimeSpan.FromMinutes(5);

    private readonly IAsyncQueryableExecuter _asyncExecuter;
    private readonly IClock _clock;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IRepository<UserLoginSession, Guid> _userLoginSessionRepository;

    public UserLoginSessionManager(
        IRepository<UserLoginSession, Guid> userLoginSessionRepository,
        IGuidGenerator guidGenerator,
        IClock clock,
        IAsyncQueryableExecuter asyncExecuter)
    {
        _userLoginSessionRepository = userLoginSessionRepository;
        _guidGenerator = guidGenerator;
        _clock = clock;
        _asyncExecuter = asyncExecuter;
    }

    public async Task<IReadOnlyList<Claim>> BuildSignInClaimsAsync(
        HttpContext httpContext,
        AbpIdentityUser user,
        string channel,
        string provider,
        IEnumerable<Claim>? extraClaims = null)
    {
        var deviceId = EnsureDeviceId(httpContext);
        var now = _clock.Now;
        var sessionKey = CreateSessionKey();
        var clientIp = GetClientIp(httpContext);
        var userAgent = GetUserAgent(httpContext);
        var currentSession = await FindCurrentSessionAsync(user.Id);

        if (currentSession == null)
        {
            currentSession = new UserLoginSession(
                _guidGenerator.Create(),
                user.TenantId,
                user.Id,
                deviceId,
                sessionKey,
                channel,
                provider,
                now,
                clientIp,
                userAgent);

            await _userLoginSessionRepository.InsertAsync(currentSession, autoSave: true);
        }
        else if (string.Equals(currentSession.DeviceId, deviceId, StringComparison.Ordinal))
        {
            currentSession.Refresh(sessionKey, channel, provider, now, clientIp, userAgent);
            await _userLoginSessionRepository.UpdateAsync(currentSession, autoSave: true);
        }
        else
        {
            currentSession.Revoke(now, UserLoginSessionConsts.RevokedBecauseReplacedByAnotherDevice);
            await _userLoginSessionRepository.UpdateAsync(currentSession, autoSave: true);

            var replacementSession = new UserLoginSession(
                _guidGenerator.Create(),
                user.TenantId,
                user.Id,
                deviceId,
                sessionKey,
                channel,
                provider,
                now,
                clientIp,
                userAgent);

            await _userLoginSessionRepository.InsertAsync(replacementSession, autoSave: true);
        }

        var claims = new List<Claim>
        {
            new(LoginSessionConstants.SessionKeyClaimType, sessionKey),
            new(LoginSessionConstants.DeviceIdClaimType, deviceId),
            new(LoginSessionConstants.LoginChannelClaimType, channel)
        };

        if (extraClaims != null)
        {
            claims.AddRange(extraClaims);
        }

        return claims;
    }

    public async Task RevokeCurrentSessionAsync(HttpContext httpContext, ClaimsPrincipal principal, string reason)
    {
        var userId = FindUserId(principal);
        var sessionKey = principal.FindFirstValue(LoginSessionConstants.SessionKeyClaimType);
        if (!userId.HasValue || string.IsNullOrWhiteSpace(sessionKey))
        {
            return;
        }

        var currentSession = await FindCurrentSessionAsync(userId.Value);
        if (currentSession == null || !string.Equals(currentSession.SessionKey, sessionKey, StringComparison.Ordinal))
        {
            return;
        }

        currentSession.Revoke(_clock.Now, reason);
        currentSession.Touch(_clock.Now, GetClientIp(httpContext), GetUserAgent(httpContext));
        await _userLoginSessionRepository.UpdateAsync(currentSession, autoSave: true);
    }

    public async Task<bool> ValidatePrincipalAsync(HttpContext httpContext, ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return true;
        }

        var userId = FindUserId(principal);
        var sessionKey = principal.FindFirstValue(LoginSessionConstants.SessionKeyClaimType);
        var principalDeviceId = principal.FindFirstValue(LoginSessionConstants.DeviceIdClaimType);
        var cookieDeviceId = TryGetDeviceId(httpContext);

        if (!userId.HasValue ||
            string.IsNullOrWhiteSpace(sessionKey) ||
            string.IsNullOrWhiteSpace(principalDeviceId) ||
            string.IsNullOrWhiteSpace(cookieDeviceId) ||
            !string.Equals(principalDeviceId, cookieDeviceId, StringComparison.Ordinal))
        {
            return false;
        }

        var currentSession = await FindCurrentSessionAsync(userId.Value);
        if (currentSession == null ||
            !currentSession.IsCurrent ||
            !string.Equals(currentSession.SessionKey, sessionKey, StringComparison.Ordinal) ||
            !string.Equals(currentSession.DeviceId, principalDeviceId, StringComparison.Ordinal))
        {
            return false;
        }

        if (_clock.Now - currentSession.LastSeenAt >= LastSeenThrottle)
        {
            currentSession.Touch(_clock.Now, GetClientIp(httpContext), GetUserAgent(httpContext));
            await _userLoginSessionRepository.UpdateAsync(currentSession, autoSave: true);
        }

        return true;
    }

    private async Task<UserLoginSession?> FindCurrentSessionAsync(Guid userId)
    {
        var query = await _userLoginSessionRepository.GetQueryableAsync();
        return await _asyncExecuter.FirstOrDefaultAsync(
            query.Where(x => x.UserId == userId && x.IsCurrent));
    }

    private string EnsureDeviceId(HttpContext httpContext)
    {
        var existingDeviceId = TryGetDeviceId(httpContext);
        if (!string.IsNullOrWhiteSpace(existingDeviceId))
        {
            return existingDeviceId;
        }

        var deviceId = _guidGenerator.Create().ToString("N");
        httpContext.Response.Cookies.Append(
            LoginSessionConstants.DeviceIdCookieName,
            deviceId,
            CreateDeviceCookieOptions(httpContext));

        return deviceId;
    }

    private static string? TryGetDeviceId(HttpContext httpContext)
    {
        return httpContext.Request.Cookies.TryGetValue(LoginSessionConstants.DeviceIdCookieName, out var deviceId)
            ? deviceId
            : null;
    }

    private static string? GetClientIp(HttpContext httpContext)
    {
        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    private static string? GetUserAgent(HttpContext httpContext)
    {
        return httpContext.Request.Headers.TryGetValue("User-Agent", out StringValues userAgent)
            ? userAgent.ToString()
            : null;
    }

    private static CookieOptions CreateDeviceCookieOptions(HttpContext httpContext)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = httpContext.Request.IsHttps,
            Expires = DateTimeOffset.UtcNow.AddYears(2)
        };
    }

    private string CreateSessionKey()
    {
        return string.Concat(
            _guidGenerator.Create().ToString("N"),
            _guidGenerator.Create().ToString("N"));
    }

    private static Guid? FindUserId(ClaimsPrincipal principal)
    {
        var rawValue = principal.FindFirstValue(AbpClaimTypes.UserId) ??
                       principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(rawValue, out var userId)
            ? userId
            : null;
    }
}
