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

        var productIds = request.Items.Select(item => item.ProductId).Distinct().ToList();
        var productItems = await products.GetByIdsAsync(productIds);

        if (productItems.Count != productIds.Count)
        {
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

        foreach (var item in request.Items)
        {
            var product = productItems.First(product => product.ProductId == item.ProductId);
            if (!product.IsAvailable || product.UnitInStock <= 0 || item.Quantity <= 0)
            {
                return null;
            }

            var unitPrice = product.DiscountPrice ?? product.Price;
            order.OrderDetails.Add(new OrderDetail
            {
                ProductId = product.ProductId,
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
        return await GetOrderDtoAsync(order.OrderId);
    }

    public async Task<List<OrderDto>> GetUserOrdersAsync(int userId)
    {
        var items = await orders.GetUserOrdersAsync(userId);
        return items.Select(CafeDtoMapper.ToOrderDto).ToList();
    }

    private async Task<OrderDto?> GetOrderDtoAsync(int orderId)
    {
        var order = await orders.GetByIdWithDetailsAsync(orderId);
        return order is null ? null : CafeDtoMapper.ToOrderDto(order);
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



