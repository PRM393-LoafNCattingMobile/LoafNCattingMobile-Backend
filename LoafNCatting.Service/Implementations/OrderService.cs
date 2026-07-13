using System.Data;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Service.Mappers;
using LoafNCatting.Data.Models;
using LoafNCatting.Data.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LoafNCatting.Service.Implementations;

public class OrderService(
    IOrderRepository orders,
    IProductRepository products,
    INotificationWriter notifications,
    IOrderStatusRepository orderStatuses,
    IPaymentMethodRepository paymentMethods,
    IConfiguration? configuration = null,
    IUserRepository? users = null) : IOrderService
{
    public async Task<OrderDto?> CreateOrderAsync(CreateOrderRequestDto request)
    {
        await ExpirePendingPaymentOrdersAsync(request.UserId);
        if (await orders.GetLatestPendingPaymentOrderAsync(request.UserId) is not null)
        {
            return null;
        }

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
        await orders.SaveChangesAsync();
        await notifications.CreateAsync(
            request.UserId,
            "Đặt món thành công",
            "Đơn hàng của bạn đã được tạo thành công.",
            "order");
        await NotifyStaffUsersAsync(
            "Đơn hàng mới",
            $"Khách hàng #{request.UserId} vừa tạo đơn #{order.OrderId}.",
            "order");
        await transaction.CommitAsync();
        return await GetOrderDtoAsync(order.OrderId);
    }

    public async Task<List<OrderDto>> GetUserOrdersAsync(int userId)
    {
        await ExpirePendingPaymentOrdersAsync(userId);
        var items = await orders.GetUserOrdersAsync(userId);
        return items.Select(CafeDtoMapper.ToOrderDto).ToList();
    }

    public async Task<OrderDto?> GetPendingPaymentOrderAsync(int userId)
    {
        await ExpirePendingPaymentOrdersAsync(userId);
        var order = await orders.GetLatestPendingPaymentOrderAsync(userId);
        return order is null ? null : CafeDtoMapper.ToOrderDto(order);
    }

    public async Task<List<OrderDto>> GetStaffOrdersAsync(int? statusId, DateOnly? date)
    {
        var items = (await orders.GetStaffOrdersAsync(statusId, date)).ToList();
        if (await ExpirePendingPaymentOrdersAsync(items))
        {
            items = (await orders.GetStaffOrdersAsync(statusId, date)).ToList();
        }

        return items.Select(CafeDtoMapper.ToOrderDto).ToList();
    }

    public async Task<OrderDto?> GetStaffOrderAsync(int id)
    {
        var order = await orders.GetByIdWithDetailsAsync(id);
        if (order is not null)
        {
            await ExpirePendingPaymentOrdersAsync([order]);
        }

        return order is null ? null : CafeDtoMapper.ToOrderDto(order);
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
            !CanTransition(order.OrderStatus.OrderStatusName, targetStatus.OrderStatusName) ||
            RequiresPaidOrder(order, targetStatus.OrderStatusName))
        {
            return null;
        }

        order.OrderStatusId = targetStatus.OrderStatusId;
        order.OrderStatus = targetStatus;
        order.StaffUserId = actingUserId;
        order.UpdatedAt = DateTime.UtcNow;
        if (targetStatus.OrderStatusName == PendingPaymentPolicy.CancelledOrderStatus)
        {
            PendingPaymentPolicy.RestoreReservedStock(order);
        }

        orders.Update(order);
        await orders.SaveChangesAsync();
        await notifications.CreateAsync(
            order.CustomerUserId,
            NotificationTitleForOrderStatus(targetStatus.OrderStatusName),
            NotificationContentForOrderStatus(order.OrderId, targetStatus.OrderStatusName),
            "order");
        return CafeDtoMapper.ToOrderDto(order);
    }

    private async Task<bool> ExpirePendingPaymentOrdersAsync(int userId)
    {
        var pendingOrders = await orders.GetPendingPaymentOrdersAsync(userId);
        return await ExpirePendingPaymentOrdersAsync(pendingOrders);
    }

    private async Task<bool> ExpirePendingPaymentOrdersAsync(IEnumerable<Order> pendingOrders)
    {
        var expiredOrders = new List<Order>();
        foreach (var order in pendingOrders)
        {
            if (await PendingPaymentPolicy.ExpireIfNeededAsync(order, orderStatuses, configuration))
            {
                expiredOrders.Add(order);
            }
        }

        if (expiredOrders.Count == 0)
        {
            return false;
        }

        await orders.SaveChangesAsync();
        foreach (var order in expiredOrders)
        {
            await notifications.CreateAsync(
                order.CustomerUserId,
                "Thanh toán đã hết hạn",
                $"Đơn #{order.OrderId} đã hết hạn thanh toán và được hủy.",
                "payment");
        }

        return true;
    }

    private async Task<OrderDto?> GetOrderDtoAsync(int orderId)
    {
        var order = await orders.GetByIdWithDetailsAsync(orderId);
        return order is null ? null : CafeDtoMapper.ToOrderDto(order);
    }

    private async Task NotifyStaffUsersAsync(string title, string content, string type)
    {
        if (users is null)
        {
            return;
        }

        var staffUsers = await users.GetAdminUsersAsync(roleId: null, search: null, active: true);
        foreach (var staff in staffUsers.Where(IsStaffUser))
        {
            await notifications.CreateAsync(staff.UserId, title, content, type);
        }
    }

    private static bool IsStaffUser(User user)
    {
        return string.Equals(user.Role?.RoleName, "Staff", StringComparison.OrdinalIgnoreCase);
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

    private static bool RequiresPaidOrder(Order order, string targetStatus)
    {
        return targetStatus == "Đang chuẩn bị" &&
            order.Payments.FirstOrDefault()?.PaymentStatus != "Đã thanh toán";
    }

    private static string NotificationTitleForOrderStatus(string statusName)
    {
        return statusName switch
        {
            "Đang chuẩn bị" => "Đơn hàng đang được chuẩn bị",
            "Hoàn thành" => "Đơn hàng đã hoàn thành",
            "Đã hủy" => "Đơn hàng đã bị hủy",
            _ => "Cập nhật đơn hàng"
        };
    }

    private static string NotificationContentForOrderStatus(int orderId, string statusName)
    {
        return statusName switch
        {
            "Đang chuẩn bị" => $"Đơn #{orderId} đang được nhân viên chuẩn bị.",
            "Hoàn thành" => $"Đơn #{orderId} đã hoàn thành.",
            "Đã hủy" => $"Đơn #{orderId} đã bị hủy.",
            _ => $"Đơn #{orderId} đã được cập nhật trạng thái {statusName}."
        };
    }
}



