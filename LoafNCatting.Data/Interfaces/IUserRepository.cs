using LoafNCatting.Data.Models;

namespace LoafNCatting.Data.Interfaces;

public interface IUserRepository : IGenericRepository<User>
{
    Task<IEnumerable<User>> GetAdminUsersAsync(int? roleId, string? search, bool? active);
    Task<User?> GetByIdWithRoleAsync(int id);
    Task<bool> ExistsByEmailOrPhoneAsync(string email, string phoneNumber);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByLoginAsync(string login, string phoneNumber);
    Task<User?> GetFirstStaffAsync();
}

