using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Service.Mappers;
using LoafNCatting.Service.Auth;
using LoafNCatting.Data.Models;
using LoafNCatting.Data.Interfaces;

namespace LoafNCatting.Service.Implementations;

public class AuthService(
    IUserRepository users,
    IRoleRepository roles,
    IPasswordService passwordService,
    ISessionTokenService sessionTokens) : IAuthService
{
    public async Task<AuthResponseDto?> RegisterAsync(RegisterRequestDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var phone = request.PhoneNumber.Trim();
        if (await users.ExistsByEmailOrPhoneAsync(email, phone))
        {
            return null;
        }

        var role = await roles.GetByNameAsync("Customer");
        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            PhoneNumber = phone,
            Password = passwordService.HashPassword(request.Password),
            RoleId = role.RoleId,
            Role = role,
            IsActive = true
        };

        await users.AddAsync(user);
        await users.SaveChangesAsync();
        return CafeDtoMapper.ToAuthResponse(user, sessionTokens.IssueToken(user));
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto request)
    {
        var login = request.Login.Trim().ToLowerInvariant();
        var user = await users.GetByLoginAsync(login, request.Login.Trim());

        if (user is null || !user.IsActive || !passwordService.VerifyPassword(request.Password, user.Password))
        {
            return null;
        }

        return CafeDtoMapper.ToAuthResponse(user, sessionTokens.IssueToken(user));
    }

    public Task LogoutAsync(string token)
    {
        sessionTokens.Revoke(token);
        return Task.CompletedTask;
    }
}



