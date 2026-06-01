using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Data.Repositories;

public class OrderStatusRepository(LoafNcattingDbContext context) : GenericRepository<OrderStatus>(context), IOrderStatusRepository
{
    public async Task<OrderStatus> GetByNameAsync(string name)
    {
        return await _context.OrderStatuses.FirstAsync(status => status.OrderStatusName == name);
    }
}

