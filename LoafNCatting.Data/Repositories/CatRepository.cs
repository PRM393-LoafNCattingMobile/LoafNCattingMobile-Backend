using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Data.Repositories;

public class CatRepository(LoafNcattingDbContext context) : GenericRepository<Cat>(context), ICatRepository
{
    public async Task<IEnumerable<Cat>> GetCatsAsync(string? search)
    {
        var query = _context.Cats.Include(cat => cat.Gender).Include(cat => cat.Status).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLowerInvariant();
            query = query.Where(cat =>
                cat.Name.ToLower().Contains(keyword) ||
                (cat.Breed != null && cat.Breed.ToLower().Contains(keyword)));
        }

        return await query.OrderBy(cat => cat.Name).ToListAsync();
    }

    public async Task<Cat?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Cats
            .Include(cat => cat.Gender)
            .Include(cat => cat.Status)
            .FirstOrDefaultAsync(cat => cat.CatId == id);
    }
}

