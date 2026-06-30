using LoafNCatting.Service.DTOs;

namespace LoafNCatting.Service.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetCategoriesAsync();
    Task<CategoryDto?> GetCategoryAsync(int id);
    Task<CategoryDto?> CreateCategoryAsync(AdminCategoryRequestDto request);
    Task<CategoryDto?> UpdateCategoryAsync(int id, AdminCategoryRequestDto request);
    Task<bool> DeleteCategoryAsync(int id);
}

