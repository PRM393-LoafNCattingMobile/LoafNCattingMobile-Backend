using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Service.Mappers;
using LoafNCatting.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Service.Implementations;

public class CategoryService(ICategoryRepository categories) : ICategoryService
{
    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        var items = await categories.GetAllOrderedAsync();
        return items.Select(CafeDtoMapper.ToCategoryDto).ToList();
    }
}



