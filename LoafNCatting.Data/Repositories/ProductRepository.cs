using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Data.Repositories;

public class ProductRepository(LoafNcattingDbContext context) : GenericRepository<Product>(context), IProductRepository
{
    public async Task<IEnumerable<Product>> GetProductsAsync(int? categoryId, string? search)
    {
        var query = _context.Products.Include(product => product.Category).AsQueryable();
        if (categoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLowerInvariant();
            query = query.Where(product => product.Name.ToLower().Contains(keyword));
        }

        return await query.OrderBy(product => product.Name).ToListAsync();
    }

    public async Task<Product?> GetByIdWithCategoryAsync(int id)
    {
        return await _context.Products
            .Include(product => product.Category)
            .FirstOrDefaultAsync(product => product.ProductId == id);
    }

    public async Task<List<Product>> GetByIdsAsync(IEnumerable<int> ids)
    {
        var productIds = ids.Distinct().ToList();
        return await _context.Products
            .Where(product => productIds.Contains(product.ProductId))
            .ToListAsync();
    }
}

