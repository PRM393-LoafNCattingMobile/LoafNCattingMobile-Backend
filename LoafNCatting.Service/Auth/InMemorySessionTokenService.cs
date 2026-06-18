using System.Security.Cryptography;
using LoafNCatting.Data.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;

namespace LoafNCatting.Service.Auth;

public sealed class InMemorySessionTokenService(
    IMemoryCache cache,
    SessionTokenOptions options) : ISessionTokenService
{
    public string IssueToken(User user)
    {
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var expiresAtUtc = DateTime.UtcNow.AddHours(options.ExpiresInHours <= 0 ? 12 : options.ExpiresInHours);
        var session = new UserSession(
            user.UserId,
            user.Role?.RoleName ?? string.Empty,
            expiresAtUtc);

        cache.Set(GetCacheKey(token), session, expiresAtUtc);
        return token;
    }

    public UserSession? GetSession(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        return cache.TryGetValue(GetCacheKey(token), out UserSession? session) ? session : null;
    }

    public void Revoke(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        cache.Remove(GetCacheKey(token));
    }

    private static string GetCacheKey(string token) => $"session:{token}";
}
