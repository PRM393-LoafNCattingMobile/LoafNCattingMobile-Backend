using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Data.Repositories;

public class RoleRepository(LoafNcattingDbContext context) : GenericRepository<Role>(context), IRoleRepository
{
    public async Task<Role> GetByNameAsync(string roleName)
    {
        return await _context.Roles.FirstAsync(role => role.RoleName == roleName);
    }
}

