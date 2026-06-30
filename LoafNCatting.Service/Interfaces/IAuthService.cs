using LoafNCatting.Service.DTOs;

namespace LoafNCatting.Service.Interfaces;

public interface IAuthService
{
    Task<EmailVerificationChallengeDto?> RegisterAsync(RegisterRequestDto request);
    Task<LoginResultDto> LoginAsync(LoginRequestDto request);
    Task<AuthResponseDto?> VerifyEmailAsync(VerifyEmailRequestDto request);
    Task<EmailVerificationChallengeDto?> ResendVerificationAsync(ResendVerificationRequestDto request);
    Task LogoutAsync(string token);
}

