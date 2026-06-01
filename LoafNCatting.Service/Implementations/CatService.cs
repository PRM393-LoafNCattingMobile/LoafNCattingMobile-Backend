using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Service.Mappers;
using LoafNCatting.Data.Interfaces;

namespace LoafNCatting.Service.Implementations;

public class CatService(ICatRepository cats) : ICatService
{
    public async Task<List<CatDto>> GetCatsAsync(string? search)
    {
        var items = await cats.GetCatsAsync(search);
        return items.Select(CafeDtoMapper.ToCatDto).ToList();
    }

    public async Task<CatDto?> GetCatAsync(int id)
    {
        var cat = await cats.GetByIdWithDetailsAsync(id);
        return cat is null ? null : CafeDtoMapper.ToCatDto(cat);
    }
}



