using LoafNCatting.Service.DTOs;

namespace LoafNCatting.Service.Interfaces;

public interface ITableService
{
    Task<List<TableDto>> GetAvailableTablesAsync(DateOnly date, TimeOnly time, int guestCount);
}

