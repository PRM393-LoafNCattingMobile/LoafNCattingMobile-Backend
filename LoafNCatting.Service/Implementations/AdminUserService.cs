using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Service.Validation;
using System.Net.Mail;

namespace LoafNCatting.Service.Implementations;

public class AdminUserService(
    IUserRepository users,
    IRoleRepository roles,
    IPasswordService passwordService,
    IMediaStorageService? mediaStorage = null) : IAdminUserService
{
    private readonly IMediaStorageService _mediaStorage =
        mediaStorage ?? PassThroughMediaStorageService.Instance;

    public async Task<List<AdminUserDto>> GetUsersAsync(
        int? roleId,
        string? search,
        bool? active)
    {
        var items = await users.GetAdminUsersAsync(roleId, search, active);
        return items.Select(ToAdminUserDto).ToList();
    }

    public async Task<AdminUserDto?> CreateStaffAsync(AdminCreateStaffDto request)
    {
        var name = request.Name.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        var phoneNumber = request.PhoneNumber.Trim();
        if (string.IsNullOrWhiteSpace(name) ||
            name.Length > 255 ||
            string.IsNullOrWhiteSpace(email) ||
            email.Length > 255 ||
            !MailAddress.TryCreate(email, out var parsedEmail) ||
            !string.Equals(parsedEmail.Address, email, StringComparison.OrdinalIgnoreCase) ||
            !PhoneNumberValidator.IsValid(phoneNumber) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        if (await users.ExistsByEmailOrPhoneAsync(email, phoneNumber))
        {
            return null;
        }

        var role = await roles.GetByNameAsync("Staff");
        var user = new User
        {
            Name = name,
            Email = email,
            PhoneNumber = phoneNumber,
            Password = passwordService.HashPassword(request.Password),
            Address = request.Address?.Trim(),
            AvatarUrl = _mediaStorage.NormalizeStoredKey(request.AvatarUrl),
            RoleId = role.RoleId,
            Role = role,
            IsActive = true,
            IsEmailVerified = true
        };

        await users.AddAsync(user);
        await users.SaveChangesAsync();
        return ToAdminUserDto(user);
    }

    public async Task<AdminUserDto?> UpdateRoleAsync(
        int id,
        AdminUserRoleDto request)
    {
        var user = await users.GetByIdWithRoleAsync(id);
        var targetRole = await roles.GetByIdAsync(request.RoleId);
        if (user is null ||
            targetRole is null ||
            user.Role.RoleName == "Admin" ||
            targetRole.RoleName is not ("Staff" or "Customer"))
        {
            return null;
        }

        user.RoleId = targetRole.RoleId;
        user.Role = targetRole;
        user.UpdatedAt = DateTime.UtcNow;
        users.Update(user);
        await users.SaveChangesAsync();
        return ToAdminUserDto(user);
    }

    public async Task<AdminUserDto?> UpdateActiveAsync(
        int id,
        AdminUserActiveDto request)
    {
        var user = await users.GetByIdWithRoleAsync(id);
        if (user is null || user.Role.RoleName == "Admin")
        {
            return null;
        }

        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        users.Update(user);
        await users.SaveChangesAsync();
        return ToAdminUserDto(user);
    }

    private AdminUserDto ToAdminUserDto(User user)
    {
        return new AdminUserDto(
            user.UserId,
            user.Name,
            user.Email,
            user.PhoneNumber,
            user.Address,
            _mediaStorage.ResolveDisplayUrl(user.AvatarUrl),
            user.RoleId,
            user.Role.RoleName,
            user.IsActive,
            user.IsEmailVerified,
            user.CreatedAt,
            user.UpdatedAt,
            _mediaStorage.NormalizeStoredKey(user.AvatarUrl));
    }
}
