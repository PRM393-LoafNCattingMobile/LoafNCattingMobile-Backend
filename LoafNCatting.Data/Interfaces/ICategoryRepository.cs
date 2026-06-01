using LoafNCatting.Data.Models;

namespace LoafNCatting.Data.Interfaces;

public interface ICategoryRepository : IGenericRepository<Category>
{
    Task<IEnumerable<Category>> GetAllOrderedAsync();
}

