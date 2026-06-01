using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Data.Repositories;

public class CategoryRepository(LoafNcattingDbContext context) : GenericRepository<Category>(context), ICategoryRepository
{
    public async Task<IEnumerable<Category>> GetAllOrderedAsync()
    {
        return await _context.Categories.OrderBy(category => category.Name).ToListAsync();
    }
}

