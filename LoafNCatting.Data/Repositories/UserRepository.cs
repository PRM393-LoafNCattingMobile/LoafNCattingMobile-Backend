using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Data.Repositories;

public class UserRepository(LoafNcattingDbContext context) : GenericRepository<User>(context), IUserRepository
{
    public async Task<IEnumerable<User>> GetAdminUsersAsync(
        int? roleId,
        string? search,
        bool? active)
    {
        var query = _context.Users
            .Include(user => user.Role)
            .AsQueryable();

        if (roleId.HasValue)
        {
            query = query.Where(user => user.RoleId == roleId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(user =>
                user.Name.Contains(term) ||
                user.Email.Contains(term) ||
                user.PhoneNumber.Contains(term));
        }

        if (active.HasValue)
        {
            query = query.Where(user => user.IsActive == active.Value);
        }

        return await query
            .OrderByDescending(user => user.CreatedAt)
            .ToListAsync();
    }

    public async Task<User?> GetByIdWithRoleAsync(int id)
    {
        return await _context.Users
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user => user.UserId == id);
    }

    public async Task<bool> ExistsByEmailOrPhoneAsync(string email, string phoneNumber)
    {
        return await _context.Users.AnyAsync(user =>
            user.Email == email ||
            user.PhoneNumber == phoneNumber);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user => user.Email == email);
    }

    public async Task<User?> GetByLoginAsync(string login, string phoneNumber)
    {
        return await _context.Users
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user => user.Email == login || user.PhoneNumber == phoneNumber);
    }

    public async Task<User?> GetFirstStaffAsync()
    {
        return await _context.Users
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user => user.Role.RoleName == "Staff");
    }
}

