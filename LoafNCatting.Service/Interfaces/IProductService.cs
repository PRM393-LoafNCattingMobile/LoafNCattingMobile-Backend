using LoafNCatting.Service.DTOs;

namespace LoafNCatting.Service.Interfaces;

public interface IProductService
{
    Task<List<ProductDto>> GetProductsAsync(int? categoryId, string? search);
    Task<ProductDto?> GetProductAsync(int id);
    Task<ProductDto?> CreateProductAsync(AdminProductRequestDto request);
    Task<ProductDto?> UpdateProductAsync(int id, AdminProductRequestDto request);
    Task<ProductDto?> UpdateAvailabilityAsync(int id, StaffProductAvailabilityDto request);
    Task<bool> DeleteProductAsync(int id);
}

