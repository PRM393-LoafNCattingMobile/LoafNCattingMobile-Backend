using LoafNCatting.Data.Models;

namespace LoafNCatting.Data.Interfaces;

public interface IOrderRepository : IGenericRepository<Order>
{
    Task<IEnumerable<Order>> GetUserOrdersAsync(int userId);
    Task<Order?> GetByIdWithDetailsAsync(int orderId);
}

