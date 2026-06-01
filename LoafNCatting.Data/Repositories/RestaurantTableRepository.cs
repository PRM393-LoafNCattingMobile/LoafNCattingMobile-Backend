using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Data.Repositories;

public class RestaurantTableRepository(
    LoafNcattingDbContext context,
    IReservationRepository reservations) : GenericRepository<RestaurantTable>(context), IRestaurantTableRepository
{
    public async Task<IEnumerable<RestaurantTable>> GetAvailableTablesAsync(DateOnly date, TimeOnly time, int guestCount)
    {
        var unavailableTableIds = await reservations.GetUnavailableTableIdsAsync(date, time);
        return await _context.RestaurantTables
            .Include(table => table.TableStatus)
            .Where(table =>
                table.Capacity >= guestCount &&
                table.TableStatus.StatusName == "Trống" &&
                !unavailableTableIds.Contains(table.TableId))
            .OrderBy(table => table.Capacity)
            .ToListAsync();
    }
}

