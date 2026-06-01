using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Service.Mappers;
using LoafNCatting.Data.Interfaces;

namespace LoafNCatting.Service.Implementations;

public class ProductService(IProductRepository products) : IProductService
{
    public async Task<List<ProductDto>> GetProductsAsync(int? categoryId, string? search)
    {
        var items = await products.GetProductsAsync(categoryId, search);
        return items.Select(CafeDtoMapper.ToProductDto).ToList();
    }

    public async Task<ProductDto?> GetProductAsync(int id)
    {
        var product = await products.GetByIdWithCategoryAsync(id);
        return product is null ? null : CafeDtoMapper.ToProductDto(product);
    }
}



