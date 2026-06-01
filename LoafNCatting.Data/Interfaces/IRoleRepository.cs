using LoafNCatting.Data.Models;

namespace LoafNCatting.Data.Interfaces;

public interface IRoleRepository : IGenericRepository<Role>
{
    Task<Role> GetByNameAsync(string roleName);
}

