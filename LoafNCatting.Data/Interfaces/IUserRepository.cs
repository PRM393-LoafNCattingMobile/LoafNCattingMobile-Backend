using LoafNCatting.Data.Models;

namespace LoafNCatting.Data.Interfaces;

public interface IUserRepository : IGenericRepository<User>
{
    Task<bool> ExistsByEmailOrPhoneAsync(string email, string phoneNumber);
    Task<User?> GetByLoginAsync(string login, string phoneNumber);
    Task<User?> GetFirstStaffAsync();
}

