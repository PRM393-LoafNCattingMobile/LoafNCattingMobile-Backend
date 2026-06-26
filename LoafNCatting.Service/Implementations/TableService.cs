using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Service.Mappers;
using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;

namespace LoafNCatting.Service.Implementations;

public class TableService(
    IRestaurantTableRepository tables,
    ITableStatusRepository tableStatuses) : ITableService
{
    public async Task<List<TableDto>> GetAvailableTablesAsync(DateOnly date, TimeOnly time, int guestCount)
    {
        var items = await tables.GetAvailableTablesAsync(date, time, guestCount);
        return items.Select(CafeDtoMapper.ToTableDto).ToList();
    }

    public async Task<List<TableDto>> GetTablesAsync()
    {
        var items = await tables.GetTablesAsync();
        return items
            .Select(CafeDtoMapper.ToTableDto)
            .ToList();
    }

    public async Task<TableDto?> GetTableAsync(int id)
    {
        var table = await tables.GetByIdWithStatusAsync(id);
        return table is null ? null : CafeDtoMapper.ToTableDto(table);
    }

    public async Task<TableDto?> CreateTableAsync(AdminTableRequestDto request)
    {
        var status = await tableStatuses.GetByIdAsync(request.TableStatusId);
        if (!IsValidRequest(request) || status is null)
        {
            return null;
        }

        var table = new RestaurantTable
        {
            TableName = request.TableName.Trim(),
            Capacity = request.Capacity,
            Area = NormalizeOptional(request.Area),
            Description = NormalizeOptional(request.Description),
            TableStatusId = status.TableStatusId,
            TableStatus = status
        };

        await tables.AddAsync(table);
        await tables.SaveChangesAsync();
        return CafeDtoMapper.ToTableDto(table);
    }

    public async Task<TableDto?> UpdateTableAsync(int id, AdminTableRequestDto request)
    {
        var table = await tables.GetByIdWithStatusAsync(id);
        var status = await tableStatuses.GetByIdAsync(request.TableStatusId);
        if (table is null || !IsValidRequest(request) || status is null)
        {
            return null;
        }

        table.TableName = request.TableName.Trim();
        table.Capacity = request.Capacity;
        table.Area = NormalizeOptional(request.Area);
        table.Description = NormalizeOptional(request.Description);
        table.TableStatusId = status.TableStatusId;
        table.TableStatus = status;

        tables.Update(table);
        await tables.SaveChangesAsync();
        return CafeDtoMapper.ToTableDto(table);
    }

    public async Task<TableDto?> UpdateTableStatusAsync(int id, StaffTableStatusDto request)
    {
        var table = await tables.GetByIdWithStatusAsync(id);
        var status = await tableStatuses.GetByIdAsync(request.TableStatusId);
        if (table is null || status is null)
        {
            return null;
        }

        table.TableStatusId = status.TableStatusId;
        table.TableStatus = status;

        tables.Update(table);
        await tables.SaveChangesAsync();
        return CafeDtoMapper.ToTableDto(table);
    }

    public async Task<bool> DeleteTableAsync(int id)
    {
        var table = await tables.GetByIdWithStatusAsync(id);
        if (table is null)
        {
            return false;
        }

        tables.Delete(table);
        await tables.SaveChangesAsync();
        return true;
    }

    private static bool IsValidRequest(AdminTableRequestDto request)
    {
        return !string.IsNullOrWhiteSpace(request.TableName) && request.Capacity > 0;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}



