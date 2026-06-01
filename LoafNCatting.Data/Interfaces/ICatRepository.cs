using LoafNCatting.Data.Models;

namespace LoafNCatting.Data.Interfaces;

public interface ICatRepository : IGenericRepository<Cat>
{
    Task<IEnumerable<Cat>> GetCatsAsync(string? search);
    Task<Cat?> GetByIdWithDetailsAsync(int id);
}

