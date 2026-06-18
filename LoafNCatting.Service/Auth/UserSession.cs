namespace LoafNCatting.Service.Auth;

public sealed record UserSession(int UserId, string RoleName, DateTime ExpiresAtUtc);
