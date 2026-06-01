using LoafNCatting.Service.DTOs;

namespace LoafNCatting.Service.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetCategoriesAsync();
}

