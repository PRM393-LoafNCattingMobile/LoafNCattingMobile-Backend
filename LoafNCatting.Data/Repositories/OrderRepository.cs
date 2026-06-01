using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Data.Repositories;

public class OrderRepository(LoafNcattingDbContext context) : GenericRepository<Order>(context), IOrderRepository
{
    public async Task<IEnumerable<Order>> GetUserOrdersAsync(int userId)
    {
        return await IncludeDetails(_context.Orders)
            .Where(order => order.CustomerUserId == userId)
            .OrderByDescending(order => order.OrderDate)
            .ToListAsync();
    }

    public async Task<Order?> GetByIdWithDetailsAsync(int orderId)
    {
        return await IncludeDetails(_context.Orders)
            .FirstOrDefaultAsync(order => order.OrderId == orderId);
    }

    private static IQueryable<Order> IncludeDetails(IQueryable<Order> query)
    {
        return query.Include(order => order.OrderStatus)
            .Include(order => order.OrderDetails)
            .ThenInclude(detail => detail.Product)
            .Include(order => order.Payments);
    }
}

