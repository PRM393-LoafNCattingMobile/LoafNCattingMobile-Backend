using LoafNCatting.Service.DTOs;

namespace LoafNCatting.Service.Interfaces;

public interface IProductService
{
    Task<List<ProductDto>> GetProductsAsync(int? categoryId, string? search);
    Task<ProductDto?> GetProductAsync(int id);
}

