using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;

namespace LoafNCatting.Service.Implementations;

public class CartService(
    ICartRepository carts,
    ICartItemRepository cartItems,
    IProductRepository products,
    IMediaStorageService? mediaStorage = null) : ICartService
{
    private readonly IMediaStorageService _mediaStorage =
        mediaStorage ?? PassThroughMediaStorageService.Instance;

    public async Task<CartDto> GetCartAsync(int userId)
    {
        var cart = await carts.GetByUserIdWithItemsAsync(userId);
        return cart is null ? EmptyCart(userId) : ToCartDto(cart);
    }

    public async Task<CartDto?> AddItemAsync(CartItemRequestDto request)
    {
        if (request.Quantity <= 0)
        {
            return await GetCartAsync(request.UserId);
        }

        var product = await products.GetByIdWithCategoryAsync(request.ProductId);
        if (!CanAdd(product))
        {
            return null;
        }

        var cart = await GetOrCreateCartAsync(request.UserId);
        var item = cart.CartItems.FirstOrDefault(item => item.ProductId == request.ProductId);
        var currentQuantity = item?.Quantity ?? 0;
        var nextQuantity = Math.Min(currentQuantity + request.Quantity, product!.UnitInStock);

        if (item is null)
        {
            item = NewCartItem(cart, product!, nextQuantity);
            cart.CartItems.Add(item);
        }
        else
        {
            UpdateCartItem(item, product!, nextQuantity);
        }

        Touch(cart);
        await carts.SaveChangesAsync();
        return ToCartDto(cart);
    }

    public async Task<CartDto?> UpdateItemAsync(CartItemRequestDto request)
    {
        var cart = await carts.GetByUserIdWithItemsAsync(request.UserId);
        if (cart is null)
        {
            return request.Quantity <= 0 ? EmptyCart(request.UserId) : await AddItemAsync(request);
        }

        var item = cart.CartItems.FirstOrDefault(item => item.ProductId == request.ProductId);
        if (request.Quantity <= 0)
        {
            if (item is not null)
            {
                RemoveItem(cart, item);
                await carts.SaveChangesAsync();
            }

            return ToCartDto(cart);
        }

        var product = await products.GetByIdWithCategoryAsync(request.ProductId);
        if (!CanAdd(product))
        {
            return null;
        }

        var nextQuantity = Math.Min(request.Quantity, product!.UnitInStock);
        if (item is null)
        {
            item = NewCartItem(cart, product, nextQuantity);
            cart.CartItems.Add(item);
        }
        else
        {
            UpdateCartItem(item, product, nextQuantity);
        }

        Touch(cart);
        await carts.SaveChangesAsync();
        return ToCartDto(cart);
    }

    public async Task<CartDto> RemoveItemAsync(int userId, int productId)
    {
        var cart = await carts.GetByUserIdWithItemsAsync(userId);
        if (cart is null)
        {
            return EmptyCart(userId);
        }

        var item = cart.CartItems.FirstOrDefault(item => item.ProductId == productId);
        if (item is not null)
        {
            RemoveItem(cart, item);
            await carts.SaveChangesAsync();
        }

        return ToCartDto(cart);
    }

    public async Task<CartDto> ClearCartAsync(int userId)
    {
        var cart = await carts.GetByUserIdWithItemsAsync(userId);
        if (cart is null)
        {
            return EmptyCart(userId);
        }

        foreach (var item in cart.CartItems.ToList())
        {
            RemoveItem(cart, item);
        }

        await carts.SaveChangesAsync();
        return ToCartDto(cart);
    }

    private async Task<Cart> GetOrCreateCartAsync(int userId)
    {
        var cart = await carts.GetByUserIdWithItemsAsync(userId);
        if (cart is not null)
        {
            return cart;
        }

        cart = new Cart
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        await carts.AddAsync(cart);
        return cart;
    }

    private static bool CanAdd(Product? product)
    {
        return product is { IsAvailable: true, UnitInStock: > 0 };
    }

    private static CartItem NewCartItem(Cart cart, Product product, int quantity)
    {
        return new CartItem
        {
            Cart = cart,
            CartId = cart.CartId,
            ProductId = product.ProductId,
            Product = product,
            Quantity = quantity,
            UnitPrice = product.DiscountPrice ?? product.Price,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static void UpdateCartItem(CartItem item, Product product, int quantity)
    {
        item.Product = product;
        item.ProductId = product.ProductId;
        item.Quantity = quantity;
        item.UnitPrice = product.DiscountPrice ?? product.Price;
        item.UpdatedAt = DateTime.UtcNow;
    }

    private void RemoveItem(Cart cart, CartItem item)
    {
        cart.CartItems.Remove(item);
        cartItems.Delete(item);
        Touch(cart);
    }

    private static void Touch(Cart cart)
    {
        cart.UpdatedAt = DateTime.UtcNow;
    }

    private CartDto ToCartDto(Cart cart)
    {
        var items = cart.CartItems
            .OrderBy(item => item.CreatedAt)
            .Select(item => new CartItemDto(
                ToProductDto(item.Product),
                item.Quantity,
                item.UnitPrice,
                item.UnitPrice * item.Quantity))
            .ToList();

        return new CartDto(
            cart.CartId,
            cart.UserId,
            items.Sum(item => item.Quantity),
            items.Sum(item => item.Subtotal),
            items);
    }

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

    private static CartDto EmptyCart(int userId)
    {
        return new CartDto(0, userId, 0, 0m, []);
    }
}
