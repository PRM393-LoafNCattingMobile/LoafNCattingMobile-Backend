using LoafNCatting.Service.DTOs;

namespace LoafNCatting.Service.Interfaces;

public interface ICatService
{
    Task<List<CatDto>> GetCatsAsync(string? search);
    Task<CatDto?> GetCatAsync(int id);
}

