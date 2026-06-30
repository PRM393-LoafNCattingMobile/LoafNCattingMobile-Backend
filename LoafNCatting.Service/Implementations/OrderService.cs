using System.Data;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Service.Mappers;
using LoafNCatting.Data.Models;
using LoafNCatting.Data.Interfaces;

namespace LoafNCatting.Service.Implementations;

public class OrderService(
    IOrderRepository orders,
    IProductRepository products,
    INotificationRepository notifications,
    IOrderStatusRepository orderStatuses,
    IPaymentMethodRepository paymentMethods) : IOrderService
{
    public async Task<OrderDto?> CreateOrderAsync(CreateOrderRequestDto request)
    {
        if (request.Items.Count == 0)
        {
            return null;
        }

        var requestedItems = request.Items
            .GroupBy(item => item.ProductId)
            .Select(group => new OrderItemRequestDto(group.Key, group.Sum(item => item.Quantity)))
            .ToList();

        if (requestedItems.Any(item => item.Quantity <= 0))
        {
            return null;
        }

        var productIds = requestedItems.Select(item => item.ProductId).ToList();
        await using var transaction = await orders.BeginTransactionAsync(IsolationLevel.Serializable);
        var productItems = await products.GetByIdsAsync(productIds);

        if (productItems.Count != productIds.Count)
        {
            await transaction.RollbackAsync();
            return null;
        }

        foreach (var item in requestedItems)
        {
            var product = productItems.First(product => product.ProductId == item.ProductId);
            if (!product.IsAvailable || product.UnitInStock < item.Quantity)
            {
                await transaction.RollbackAsync();
                return null;
            }
        }

        var stockReserved = await products.TryReserveStockAsync(
            requestedItems.ToDictionary(item => item.ProductId, item => item.Quantity));
        if (!stockReserved)
        {
            await transaction.RollbackAsync();
            return null;
        }

        var status = await orderStatuses.GetByNameAsync("Đang chờ");
        var method = await paymentMethods.GetByNameOrDefaultAsync(request.PaymentMethod);

        var order = new Order
        {
            CustomerUserId = request.UserId,
            TableId = request.TableId,
            ReservationId = request.ReservationId,
            OrderType = request.OrderType,
            Note = request.Note,
            OrderStatusId = status.OrderStatusId
        };

        foreach (var item in requestedItems)
        {
            var product = productItems.First(product => product.ProductId == item.ProductId);
            var unitPrice = product.DiscountPrice ?? product.Price;
            order.OrderDetails.Add(new OrderDetail
            {
                ProductId = product.ProductId,
                Product = product,
                Quantity = item.Quantity,
                UnitPrice = unitPrice,
                Subtotal = unitPrice * item.Quantity
            });

        }

        order.TotalPrice = order.OrderDetails.Sum(item => item.Subtotal);

        // Đơn chuyển khoản đi qua PayOS nên để trạng thái "Đang chờ thanh toán";
        // các phương thức còn lại (tiền mặt...) coi như đã thanh toán ngay như demo cũ.
        var requiresOnlinePayment = method.MethodName.Contains("Chuyển khoản", StringComparison.OrdinalIgnoreCase);
        order.Payments.Add(new Payment
        {
            PaymentAmount = order.TotalPrice,
            MethodId = method.MethodId,
            PaymentStatus = requiresOnlinePayment ? "Đang chờ thanh toán" : "Đã thanh toán",
            TransactionCode = requiresOnlinePayment ? null : $"DEMO-{DateTime.UtcNow:yyyyMMddHHmmss}",
            PaidAt = requiresOnlinePayment ? null : DateTime.UtcNow
        });

        await orders.AddAsync(order);
        await AddNotificationAsync(request.UserId, "Đặt món thành công", "Đơn hàng của bạn đã được tạo thành công.", "order");
        await orders.SaveChangesAsync();
        await transaction.CommitAsync();
        return await GetOrderDtoAsync(order.OrderId);
    }

    public async Task<List<OrderDto>> GetUserOrdersAsync(int userId)
    {
        var items = await orders.GetUserOrdersAsync(userId);
        return items.Select(CafeDtoMapper.ToOrderDto).ToList();
    }

    public async Task<List<OrderDto>> GetStaffOrdersAsync(int? statusId, DateOnly? date)
    {
        var items = await orders.GetStaffOrdersAsync(statusId, date);
        return items.Select(CafeDtoMapper.ToOrderDto).ToList();
    }

    public async Task<OrderDto?> UpdateOrderStatusAsync(
        int id,
        int actingUserId,
        StaffOrderStatusDto request)
    {
        var order = await orders.GetByIdWithDetailsAsync(id);
        var targetStatus = await orderStatuses.GetByIdAsync(request.StatusId);
        if (order is null ||
            targetStatus is null ||
            !CanTransition(order.OrderStatus.OrderStatusName, targetStatus.OrderStatusName))
        {
            return null;
        }

        order.OrderStatusId = targetStatus.OrderStatusId;
        order.OrderStatus = targetStatus;
        order.StaffUserId = actingUserId;
        order.UpdatedAt = DateTime.UtcNow;
        orders.Update(order);
        await orders.SaveChangesAsync();
        return CafeDtoMapper.ToOrderDto(order);
    }

    private async Task<OrderDto?> GetOrderDtoAsync(int orderId)
    {
        var order = await orders.GetByIdWithDetailsAsync(orderId);
        return order is null ? null : CafeDtoMapper.ToOrderDto(order);
    }

    private static bool CanTransition(string currentStatus, string targetStatus)
    {
        return currentStatus switch
        {
            "Đang chờ" => targetStatus is "Đang chuẩn bị" or "Đã hủy",
            "Đang chuẩn bị" => targetStatus is "Hoàn thành" or "Đã hủy",
            _ => false
        };
    }

    private async Task AddNotificationAsync(int? userId, string title, string content, string type)
    {
        if (!userId.HasValue)
        {
            return;
        }

        await notifications.AddAsync(new Notification
        {
            UserId = userId.Value,
            Title = title,
            Content = content,
            Type = type
        });
    }
}



