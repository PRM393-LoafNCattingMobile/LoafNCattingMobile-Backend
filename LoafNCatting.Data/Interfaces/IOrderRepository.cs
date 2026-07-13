using LoafNCatting.Data.Models;

namespace LoafNCatting.Data.Interfaces;

public interface IOrderRepository : IGenericRepository<Order>
{
    Task<IEnumerable<Order>> GetUserOrdersAsync(int userId);
    Task<IEnumerable<Order>> GetStaffOrdersAsync(int? statusId, DateOnly? date);
    Task<Order?> GetByIdWithDetailsAsync(int orderId);
    Task<Order?> GetLatestPendingPaymentOrderAsync(int userId);
    Task<List<Order>> GetPendingPaymentOrdersAsync(int userId);
}

