using LoafNCatting.Data.Models;

namespace LoafNCatting.Data.Interfaces;

public interface IRestaurantTableRepository : IGenericRepository<RestaurantTable>
{
    Task<IEnumerable<RestaurantTable>> GetAvailableTablesAsync(DateOnly date, TimeOnly time, int guestCount);
    Task<IEnumerable<RestaurantTable>> GetTablesAsync();
    Task<RestaurantTable?> GetByIdWithStatusAsync(int id);
}

