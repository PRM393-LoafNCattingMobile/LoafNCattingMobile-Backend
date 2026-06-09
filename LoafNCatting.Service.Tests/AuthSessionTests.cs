using LoafNCatting.Data.Models;
using LoafNCatting.Service.Auth;
using Microsoft.Extensions.Caching.Memory;

namespace LoafNCatting.Service.Tests;

public class AuthSessionTests
{
    [Fact]
    public void InMemorySessionTokenService_IssueToken_CreatesAnActiveSession()
    {
        var service = new InMemorySessionTokenService(
            new MemoryCache(new MemoryCacheOptions()),
            new SessionTokenOptions { ExpiresInHours = 12 });
        var user = new User
        {
            UserId = 99,
            Name = "Test User",
            Email = "test@example.com",
            PhoneNumber = "0123456789",
            Role = new Role { RoleId = 1, RoleName = "Customer" }
        };

        var token = service.IssueToken(user);
        var session = service.GetSession(token);

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.NotNull(session);
        Assert.Equal(99, session!.UserId);
        Assert.Equal("Customer", session.RoleName);
        Assert.True(session.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public void InMemorySessionTokenService_Revoke_RemovesTheActiveSession()
    {
        var service = new InMemorySessionTokenService(
            new MemoryCache(new MemoryCacheOptions()),
            new SessionTokenOptions { ExpiresInHours = 12 });
        var token = service.IssueToken(new User
        {
            UserId = 7,
            Name = "Revoked User",
            Email = "revoked@example.com",
            PhoneNumber = "0987654321",
            Role = new Role { RoleId = 1, RoleName = "Customer" }
        });

        service.Revoke(token);

        Assert.Null(service.GetSession(token));
    }
}
