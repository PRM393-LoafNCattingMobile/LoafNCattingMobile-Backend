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

    public async Task<IEnumerable<Order>> GetStaffOrdersAsync(int? statusId, DateOnly? date)
    {
        var query = IncludeDetails(_context.Orders);
        if (statusId.HasValue)
        {
            query = query.Where(order => order.OrderStatusId == statusId.Value);
        }

        if (date.HasValue)
        {
            var start = date.Value.ToDateTime(TimeOnly.MinValue);
            var end = start.AddDays(1);
            query = query.Where(order => order.OrderDate >= start && order.OrderDate < end);
        }

        return await query
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
        return query.Include(order => order.CustomerUser)
            .Include(order => order.OrderStatus)
            .Include(order => order.OrderDetails)
            .ThenInclude(detail => detail.Product)
            .Include(order => order.Payments);
    }
}

