using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Service.Mappers;
using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;

namespace LoafNCatting.Service.Implementations;

public class CategoryService(
    ICategoryRepository categories,
    IProductRepository products) : ICategoryService
{
    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        var items = await categories.GetAllOrderedAsync();
        return items.Select(CafeDtoMapper.ToCategoryDto).ToList();
    }

    public async Task<CategoryDto?> GetCategoryAsync(int id)
    {
        var category = await categories.GetByIdAsync(id);
        return category is null ? null : CafeDtoMapper.ToCategoryDto(category);
    }

    public async Task<CategoryDto?> CreateCategoryAsync(AdminCategoryRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return null;
        }

        var category = new Category
        {
            Name = request.Name.Trim(),
            Description = NormalizeOptional(request.Description)
        };

        await categories.AddAsync(category);
        await categories.SaveChangesAsync();
        return CafeDtoMapper.ToCategoryDto(category);
    }

    public async Task<CategoryDto?> UpdateCategoryAsync(int id, AdminCategoryRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return null;
        }

        var category = await categories.GetByIdAsync(id);
        if (category is null)
        {
            return null;
        }

        category.Name = request.Name.Trim();
        category.Description = NormalizeOptional(request.Description);
        categories.Update(category);
        await categories.SaveChangesAsync();
        return CafeDtoMapper.ToCategoryDto(category);
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        var category = await categories.GetByIdAsync(id);
        if (category is null)
        {
            return false;
        }

        var existingProducts = await products.GetProductsAsync(id, null);
        if (existingProducts.Any())
        {
            return false;
        }

        categories.Delete(category);
        await categories.SaveChangesAsync();
        return true;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}



