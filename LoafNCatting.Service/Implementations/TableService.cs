using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Service.Mappers;
using LoafNCatting.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Service.Implementations;

public class TableService(
    IRestaurantTableRepository tables) : ITableService
{
    public async Task<List<TableDto>> GetAvailableTablesAsync(DateOnly date, TimeOnly time, int guestCount)
    {
        var items = await tables.GetAvailableTablesAsync(date, time, guestCount);
        return items.Select(CafeDtoMapper.ToTableDto).ToList();
    }
}



