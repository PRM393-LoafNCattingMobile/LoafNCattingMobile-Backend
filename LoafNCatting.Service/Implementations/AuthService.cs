using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Service.Mappers;
using LoafNCatting.Service.Auth;
using LoafNCatting.Data.Models;
using LoafNCatting.Data.Interfaces;
using Microsoft.Extensions.Options;

namespace LoafNCatting.Service.Implementations;

public class AuthService(
    IUserRepository users,
    IRoleRepository roles,
    IPasswordService passwordService,
    ISessionTokenService sessionTokens,
    IMailService mailService,
    IOtpGenerator otpGenerator,
    IVerificationEmailComposer verificationEmailComposer,
    IOptions<EmailVerificationOptions> emailVerificationOptions,
    IMediaStorageService? mediaStorage = null) : IAuthService
{
    private readonly EmailVerificationOptions _emailVerificationOptions = emailVerificationOptions.Value;
    private readonly IMediaStorageService _mediaStorage =
        mediaStorage ?? PassThroughMediaStorageService.Instance;

    public async Task<EmailVerificationChallengeDto?> RegisterAsync(RegisterRequestDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var phone = request.PhoneNumber.Trim();
        if (await users.ExistsByEmailOrPhoneAsync(email, phone))
        {
            return null;
        }

        var role = await roles.GetByNameAsync("Customer");
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_emailVerificationOptions.ExpiresInMinutes);
        var verificationCode = otpGenerator.GenerateNumericCode(_emailVerificationOptions.OtpLength);
        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            PhoneNumber = phone,
            Password = passwordService.HashPassword(request.Password),
            RoleId = role.RoleId,
            Role = role,
            IsActive = true,
            IsEmailVerified = false,
            EmailVerificationOtpHash = passwordService.HashPassword(verificationCode),
            EmailVerificationOtpExpiresAt = expiresAtUtc
        };

        await users.AddAsync(user);
        await users.SaveChangesAsync();
        await SendVerificationEmailAsync(user, verificationCode, expiresAtUtc);
        return new EmailVerificationChallengeDto(user.Email, expiresAtUtc);
    }

    public async Task<LoginResultDto> LoginAsync(LoginRequestDto request)
    {
        var login = request.Login.Trim().ToLowerInvariant();
        var user = await users.GetByLoginAsync(login, request.Login.Trim());

        if (user is null || !user.IsActive || !passwordService.VerifyPassword(request.Password, user.Password))
        {
            return new LoginResultDto(null, false, null);
        }

        if (!user.IsEmailVerified)
        {
            return new LoginResultDto(null, true, user.Email);
        }

        return new LoginResultDto(
            ToAuthResponse(user, sessionTokens.IssueToken(user)),
            false,
            null);
    }

    public async Task<AuthResponseDto?> VerifyEmailAsync(VerifyEmailRequestDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await users.GetByEmailAsync(email);

        if (user is null ||
            !user.IsActive ||
            user.IsEmailVerified ||
            string.IsNullOrWhiteSpace(user.EmailVerificationOtpHash) ||
            user.EmailVerificationOtpExpiresAt is null ||
            user.EmailVerificationOtpExpiresAt <= DateTime.UtcNow ||
            !passwordService.VerifyPassword(request.VerificationCode.Trim(), user.EmailVerificationOtpHash))
        {
            return null;
        }

        user.IsEmailVerified = true;
        user.EmailVerificationOtpHash = null;
        user.EmailVerificationOtpExpiresAt = null;
        user.UpdatedAt = DateTime.UtcNow;

        await users.SaveChangesAsync();
        return ToAuthResponse(user, sessionTokens.IssueToken(user));
    }

    public async Task<EmailVerificationChallengeDto?> ResendVerificationAsync(ResendVerificationRequestDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await users.GetByEmailAsync(email);

        if (user is null || !user.IsActive || user.IsEmailVerified)
        {
            return null;
        }

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_emailVerificationOptions.ExpiresInMinutes);
        var verificationCode = otpGenerator.GenerateNumericCode(_emailVerificationOptions.OtpLength);

        user.EmailVerificationOtpHash = passwordService.HashPassword(verificationCode);
        user.EmailVerificationOtpExpiresAt = expiresAtUtc;
        user.UpdatedAt = DateTime.UtcNow;

        await users.SaveChangesAsync();
        await SendVerificationEmailAsync(user, verificationCode, expiresAtUtc);
        return new EmailVerificationChallengeDto(user.Email, expiresAtUtc);
    }

    public Task LogoutAsync(string token)
    {
        sessionTokens.Revoke(token);
        return Task.CompletedTask;
    }

    public async Task<AuthResponseDto?> UpdateProfileAsync(int userId, UpdateProfileDto request, string token)
    {
        var user = await users.GetByIdWithRoleAsync(userId);
        var name = request.Name.Trim();
        var phoneNumber = request.PhoneNumber.Trim();
        if (user is null || !user.IsActive || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        var existingUsers = await users.GetAdminUsersAsync(roleId: null, search: null, active: null);
        if (existingUsers.Any(item =>
                item.UserId != userId &&
                string.Equals(item.PhoneNumber, phoneNumber, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        user.Name = name;
        user.PhoneNumber = phoneNumber;
        user.UpdatedAt = DateTime.UtcNow;
        users.Update(user);
        await users.SaveChangesAsync();

        return ToAuthResponse(user, token);
    }

    public async Task<AuthResponseDto?> UpdateAvatarAsync(int userId, string? s3Key, string token)
    {
        var user = await users.GetByIdWithRoleAsync(userId);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        user.AvatarUrl = _mediaStorage.NormalizeStoredKey(string.IsNullOrWhiteSpace(s3Key) ? null : s3Key.Trim());
        user.UpdatedAt = DateTime.UtcNow;
        users.Update(user);
        await users.SaveChangesAsync();

        return ToAuthResponse(user, token);
    }

    private Task SendVerificationEmailAsync(User user, string verificationCode, DateTime expiresAtUtc)
    {
        var message = verificationEmailComposer.Compose(
            user.Email,
            user.Name,
            verificationCode,
            expiresAtUtc - DateTime.UtcNow);

        return mailService.SendAsync(message);
    }

    private AuthResponseDto ToAuthResponse(User user, string token)
    {
        return new AuthResponseDto(
            user.UserId,
            user.Name,
            user.Email,
            user.PhoneNumber,
            user.Role.RoleName,
            token,
            _mediaStorage.ResolveDisplayUrl(user.AvatarUrl));
    }
}



