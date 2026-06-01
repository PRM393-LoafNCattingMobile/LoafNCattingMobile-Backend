using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Data.Repositories;

public class StoreLocationRepository(LoafNcattingDbContext context) : GenericRepository<StoreLocation>(context), IStoreLocationRepository
{
    public async Task<StoreLocation?> GetFirstAsync()
    {
        return await _context.StoreLocations.FirstOrDefaultAsync();
    }
}

