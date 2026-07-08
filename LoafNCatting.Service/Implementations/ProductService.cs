using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Service.Mappers;
using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;

namespace LoafNCatting.Service.Implementations;

public class ProductService(
    IProductRepository products,
    ICategoryRepository categories,
    IMediaStorageService? mediaStorage = null) : IProductService
{
    private readonly IMediaStorageService _mediaStorage =
        mediaStorage ?? PassThroughMediaStorageService.Instance;

    public async Task<List<ProductDto>> GetProductsAsync(int? categoryId, string? search)
    {
        var items = await products.GetProductsAsync(categoryId, search);
        return items.Select(ToProductDto).ToList();
    }

    public async Task<ProductDto?> GetProductAsync(int id)
    {
        var product = await products.GetByIdWithCategoryAsync(id);
        return product is null ? null : ToProductDto(product);
    }

    public async Task<ProductDto?> CreateProductAsync(AdminProductRequestDto request)
    {
        var category = await categories.GetByIdAsync(request.CategoryId);
        if (!IsValidRequest(request) || category is null)
        {
            return null;
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = NormalizeOptional(request.Description),
            Price = request.Price,
            DiscountPrice = request.DiscountPrice,
            UnitInStock = request.UnitInStock,
            Picture = NormalizePicture(request.Picture),
            CategoryId = category.CategoryId,
            Category = category,
            IsAvailable = request.IsAvailable,
            CreatedAt = DateTime.UtcNow
        };

        await products.AddAsync(product);
        await products.SaveChangesAsync();
        return ToProductDto(product);
    }

    public async Task<ProductDto?> UpdateProductAsync(int id, AdminProductRequestDto request)
    {
        var product = await products.GetByIdWithCategoryAsync(id);
        var category = await categories.GetByIdAsync(request.CategoryId);
        if (product is null || !IsValidRequest(request) || category is null)
        {
            return null;
        }

        product.Name = request.Name.Trim();
        product.Description = NormalizeOptional(request.Description);
        product.Price = request.Price;
        product.DiscountPrice = request.DiscountPrice;
        product.UnitInStock = request.UnitInStock;
        product.Picture = NormalizePicture(request.Picture);
        product.CategoryId = category.CategoryId;
        product.Category = category;
        product.IsAvailable = request.IsAvailable;
        product.UpdatedAt = DateTime.UtcNow;

        products.Update(product);
        await products.SaveChangesAsync();
        return ToProductDto(product);
    }

    public async Task<ProductDto?> UpdateAvailabilityAsync(int id, StaffProductAvailabilityDto request)
    {
        if (request.UnitInStock < 0)
        {
            return null;
        }

        var product = await products.GetByIdWithCategoryAsync(id);
        if (product is null)
        {
            return null;
        }

        product.UnitInStock = request.UnitInStock;
        product.IsAvailable = request.IsAvailable;
        product.UpdatedAt = DateTime.UtcNow;

        products.Update(product);
        await products.SaveChangesAsync();
        return ToProductDto(product);
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var product = await products.GetByIdAsync(id);
        if (product is null)
        {
            return false;
        }

        products.Delete(product);
        await products.SaveChangesAsync();
        return true;
    }

    private static bool IsValidRequest(AdminProductRequestDto request)
    {
        return !string.IsNullOrWhiteSpace(request.Name) &&
            request.Price >= 0 &&
            (!request.DiscountPrice.HasValue || request.DiscountPrice.Value >= 0) &&
            request.UnitInStock >= 0;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private string? NormalizePicture(string? value) =>
        _mediaStorage.NormalizeStoredKey(NormalizeOptional(value));

    private ProductDto ToProductDto(Product product)
    {
        var normalizedPictureKey = _mediaStorage.NormalizeStoredKey(product.Picture);
        return new ProductDto(
            product.ProductId,
            product.Name,
            product.Description,
            product.Price,
            product.DiscountPrice,
            product.UnitInStock,
            _mediaStorage.ResolveDisplayUrl(product.Picture),
            product.CategoryId,
            product.Category.Name,
            product.IsAvailable,
            product.IsAvailable && product.UnitInStock > 0,
            normalizedPictureKey);
    }
}



