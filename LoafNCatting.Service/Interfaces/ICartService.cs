using LoafNCatting.Service.DTOs;

namespace LoafNCatting.Service.Interfaces;

public interface ICartService
{
    Task<CartDto> GetCartAsync(int userId);
    Task<CartDto?> AddItemAsync(CartItemRequestDto request);
    Task<CartDto?> UpdateItemAsync(CartItemRequestDto request);
    Task<CartDto> RemoveItemAsync(int userId, int productId);
    Task<CartDto> ClearCartAsync(int userId);
}
