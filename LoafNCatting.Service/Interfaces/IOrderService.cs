using LoafNCatting.Service.DTOs;

namespace LoafNCatting.Service.Interfaces;

public interface IOrderService
{
    Task<OrderDto?> CreateOrderAsync(CreateOrderRequestDto request);
    Task<List<OrderDto>> GetUserOrdersAsync(int userId);
    Task<OrderDto?> GetPendingPaymentOrderAsync(int userId);
    Task<List<OrderDto>> GetStaffOrdersAsync(int? statusId, DateOnly? date);
    Task<OrderDto?> GetStaffOrderAsync(int id);
    Task<OrderDto?> UpdateOrderStatusAsync(int id, int actingUserId, StaffOrderStatusDto request);
}

