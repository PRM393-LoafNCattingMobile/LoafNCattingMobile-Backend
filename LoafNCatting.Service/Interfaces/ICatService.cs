using LoafNCatting.Service.DTOs;

namespace LoafNCatting.Service.Interfaces;

public interface ICatService
{
    Task<List<CatDto>> GetCatsAsync(string? search);
    Task<CatDto?> GetCatAsync(int id);
    Task<CatDto?> CreateCatAsync(AdminCatRequestDto request);
    Task<CatDto?> UpdateCatAsync(int id, AdminCatRequestDto request);
    Task<CatDto?> UpdateCatStatusAsync(int id, StaffCatStatusDto request);
    Task<bool> DeleteCatAsync(int id);
}

