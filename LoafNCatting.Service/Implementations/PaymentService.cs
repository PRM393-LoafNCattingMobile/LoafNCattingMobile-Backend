using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.Extensions.Configuration;
using Net.payOS.Types;

namespace LoafNCatting.Service.Implementations;

public class PaymentService(
    IPayOsClient payOS,
    IOrderRepository orders,
    IOrderStatusRepository orderStatuses,
    INotificationWriter notifications,
    IConfiguration configuration) : IPaymentService
{
    public async Task<PaymentLinkDto?> CreatePaymentLinkAsync(int orderId, int userId)
    {
        var order = await orders.GetByIdWithDetailsAsync(orderId);
        var payment = order?.Payments.FirstOrDefault();
        if (order is null || payment is null || order.CustomerUserId != userId)
        {
            return null;
        }

        if (await ExpirePendingPaymentIfNeededAsync(order))
        {
            return null;
        }

        // Chỉ đơn đang chờ thanh toán mới được tạo link PayOS.
        if (payment.PaymentStatus != PendingPaymentPolicy.PendingPaymentStatus ||
            order.OrderStatus.OrderStatusName != PendingPaymentPolicy.PendingOrderStatus)
        {
            return null;
        }

        // amount của PayOS là số nguyên VND, tính lại từ DB (không tin client).
        var amount = (int)Math.Round(order.TotalPrice);
        if (amount <= 0)
        {
            return null;
        }

        // orderCode phải là số, duy nhất mỗi link. Lưu vào TransactionCode để poll lại.
        var orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var items = order.OrderDetails
            .Select(detail => new ItemData(detail.Product.Name, detail.Quantity, (int)Math.Round(detail.UnitPrice)))
            .ToList();

        var returnUrl = configuration["PayOS:ReturnUrl"] ?? "https://payos-return.local/success";
        var cancelUrl = configuration["PayOS:CancelUrl"] ?? "https://payos-return.local/cancel";

        // PayOS giới hạn description tối đa 25 ký tự.
        var description = $"LNC don {orderId}";
        if (description.Length > 25)
        {
            description = description[..25];
        }

        var paymentData = new PaymentData(orderCode, amount, description, items, cancelUrl, returnUrl);
        var result = await payOS.CreatePaymentLinkAsync(paymentData);

        payment.TransactionCode = orderCode.ToString();
        payment.PaymentStatus = PendingPaymentPolicy.PendingPaymentStatus;
        await orders.SaveChangesAsync();

        return new PaymentLinkDto(
            orderId,
            result.orderCode,
            result.amount,
            result.checkoutUrl,
            result.qrCode,
            result.paymentLinkId);
    }

    public async Task<PaymentStatusDto?> GetPaymentStatusAsync(int orderId, int userId)
    {
        var order = await orders.GetByIdWithDetailsAsync(orderId);
        var payment = order?.Payments.FirstOrDefault();
        if (order is null || payment is null || order.CustomerUserId != userId)
        {
            return null;
        }

        // Đã đánh dấu thanh toán -> trả luôn, khỏi gọi PayOS.
        if (payment.PaymentStatus == PendingPaymentPolicy.PaidPaymentStatus)
        {
            return new PaymentStatusDto(orderId, payment.PaymentStatus, order.OrderStatus.OrderStatusName, true);
        }

        if (payment.PaymentStatus == PendingPaymentPolicy.CancelledPaymentStatus)
        {
            return new PaymentStatusDto(orderId, payment.PaymentStatus, order.OrderStatus.OrderStatusName, false);
        }

        // Chưa tạo link (chưa có orderCode) -> trả trạng thái hiện tại.
        if (!long.TryParse(payment.TransactionCode, out var orderCode))
        {
            if (await ExpirePendingPaymentIfNeededAsync(order))
            {
                return new PaymentStatusDto(orderId, payment.PaymentStatus, order.OrderStatus.OrderStatusName, false);
            }

            return new PaymentStatusDto(orderId, payment.PaymentStatus, order.OrderStatus.OrderStatusName, false);
        }

        // Hỏi PayOS trạng thái thật của link (thay cho webhook trong môi trường dev).
        var info = await payOS.GetPaymentLinkInformationAsync(orderCode);

        if (info.status == "PAID")
        {
            payment.PaymentStatus = PendingPaymentPolicy.PaidPaymentStatus;
            payment.PaidAt = DateTime.UtcNow;
            await orders.SaveChangesAsync();
            await notifications.CreateAsync(
                order.CustomerUserId,
                "Thanh toán thành công",
                $"Đơn #{orderId} đã được thanh toán thành công.",
                "payment");
            return new PaymentStatusDto(orderId, payment.PaymentStatus, order.OrderStatus.OrderStatusName, true);
        }

        if (info.status is "CANCELLED" or "EXPIRED")
        {
            await PendingPaymentPolicy.CancelPendingOrderAsync(order, payment, orderStatuses);
            await orders.SaveChangesAsync();
            await NotifyPaymentCancelledAsync(order);
        }

        if (await ExpirePendingPaymentIfNeededAsync(order))
        {
            return new PaymentStatusDto(orderId, payment.PaymentStatus, order.OrderStatus.OrderStatusName, false);
        }

        return new PaymentStatusDto(orderId, payment.PaymentStatus, order.OrderStatus.OrderStatusName, false);
    }

    private async Task<bool> ExpirePendingPaymentIfNeededAsync(Order order)
    {
        if (!await PendingPaymentPolicy.ExpireIfNeededAsync(order, orderStatuses, configuration))
        {
            return false;
        }

        await orders.SaveChangesAsync();
        await NotifyPaymentCancelledAsync(order);
        return true;
    }

    private Task<NotificationDto?> NotifyPaymentCancelledAsync(Order order)
    {
        return notifications.CreateAsync(
            order.CustomerUserId,
            "Thanh toán chưa hoàn tất",
            $"Thanh toán cho đơn #{order.OrderId} đã bị hủy hoặc hết hạn.",
            "payment");
    }
}
