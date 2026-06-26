using System.ComponentModel.DataAnnotations;

namespace LoafNCatting.Service.Auth;

public sealed class EmailVerificationOptions
{
    public const string SectionName = "Auth:EmailVerification";

    [Range(4, 10)]
    public int OtpLength { get; init; } = 6;

    [Range(1, 1440)]
    public int ExpiresInMinutes { get; init; } = 10;
}
