using LoafNCatting.Service.DTOs;

namespace LoafNCatting.Service.Interfaces;

public interface IAdminUserService
{
    Task<List<AdminUserDto>> GetUsersAsync(int? roleId, string? search, bool? active);
    Task<AdminUserDto?> CreateStaffAsync(AdminCreateStaffDto request);
    Task<AdminUserDto?> UpdateRoleAsync(int id, AdminUserRoleDto request);
    Task<AdminUserDto?> UpdateActiveAsync(int id, AdminUserActiveDto request);
}
