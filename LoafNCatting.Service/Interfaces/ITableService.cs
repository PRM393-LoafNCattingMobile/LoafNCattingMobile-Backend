using LoafNCatting.Service.DTOs;

namespace LoafNCatting.Service.Interfaces;

public interface ITableService
{
    Task<List<TableDto>> GetAvailableTablesAsync(DateOnly date, TimeOnly time, int guestCount);
    Task<List<TableDto>> GetTablesAsync();
    Task<TableDto?> GetTableAsync(int id);
    Task<TableDto?> CreateTableAsync(AdminTableRequestDto request);
    Task<TableDto?> UpdateTableAsync(int id, AdminTableRequestDto request);
    Task<TableDto?> UpdateTableStatusAsync(int id, StaffTableStatusDto request);
    Task<bool> DeleteTableAsync(int id);
}

