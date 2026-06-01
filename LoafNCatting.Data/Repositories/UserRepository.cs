using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Data.Repositories;

public class UserRepository(LoafNcattingDbContext context) : GenericRepository<User>(context), IUserRepository
{
    public async Task<bool> ExistsByEmailOrPhoneAsync(string email, string phoneNumber)
    {
        return await _context.Users.AnyAsync(user => user.Email == email || user.PhoneNumber == phoneNumber);
    }

    public async Task<User?> GetByLoginAsync(string login, string phoneNumber)
    {
        return await _context.Users
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user => user.Email.ToLower() == login || user.PhoneNumber == phoneNumber);
    }

    public async Task<User?> GetFirstStaffAsync()
    {
        return await _context.Users
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user => user.Role.RoleName == "Staff");
    }
}

