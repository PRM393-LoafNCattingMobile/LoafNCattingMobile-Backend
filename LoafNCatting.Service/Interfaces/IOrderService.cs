using LoafNCatting.Service.DTOs;

namespace LoafNCatting.Service.Interfaces;

public interface IOrderService
{
    Task<OrderDto?> CreateOrderAsync(CreateOrderRequestDto request);
    Task<List<OrderDto>> GetUserOrdersAsync(int userId);
}

