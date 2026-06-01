using LoafNCatting.Data.Models;

namespace LoafNCatting.Data.Interfaces;

public interface IOrderStatusRepository : IGenericRepository<OrderStatus>
{
    Task<OrderStatus> GetByNameAsync(string name);
}

