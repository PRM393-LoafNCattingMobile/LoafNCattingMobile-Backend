using LoafNCatting.Service.DTOs;

namespace LoafNCatting.Service.Interfaces;

public interface IAuthService
{
    Task<EmailVerificationChallengeDto?> RegisterAsync(RegisterRequestDto request);
    Task<LoginResultDto> LoginAsync(LoginRequestDto request);
    Task<AuthResponseDto?> VerifyEmailAsync(VerifyEmailRequestDto request);
    Task<EmailVerificationChallengeDto?> ResendVerificationAsync(ResendVerificationRequestDto request);
    Task<AuthResponseDto?> UpdateProfileAsync(int userId, UpdateProfileDto request, string token);
    Task<AuthResponseDto?> UpdateAvatarAsync(int userId, string? s3Key, string token);
    Task LogoutAsync(string token);
}

