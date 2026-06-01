using LoafNCatting.Data.Models;

namespace LoafNCatting.Data.Interfaces;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<IEnumerable<Product>> GetProductsAsync(int? categoryId, string? search);
    Task<Product?> GetByIdWithCategoryAsync(int id);
    Task<List<Product>> GetByIdsAsync(IEnumerable<int> ids);
}

