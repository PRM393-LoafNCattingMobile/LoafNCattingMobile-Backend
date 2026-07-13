using LoafNCatting.Data.Interfaces;
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

        // Đã thanh toán rồi thì không tạo link mới.
        if (payment.PaymentStatus == "Đã thanh toán")
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
        payment.PaymentStatus = "Đang chờ thanh toán";
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
        if (payment.PaymentStatus == "Đã thanh toán")
        {
            return new PaymentStatusDto(orderId, payment.PaymentStatus, order.OrderStatus.OrderStatusName, true);
        }

        // Chưa tạo link (chưa có orderCode) -> trả trạng thái hiện tại.
        if (!long.TryParse(payment.TransactionCode, out var orderCode))
        {
            return new PaymentStatusDto(orderId, payment.PaymentStatus, order.OrderStatus.OrderStatusName, false);
        }

        // Hỏi PayOS trạng thái thật của link (thay cho webhook trong môi trường dev).
        var info = await payOS.GetPaymentLinkInformationAsync(orderCode);

        if (info.status == "PAID")
        {
            payment.PaymentStatus = "Đã thanh toán";
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
            payment.PaymentStatus = "Đã hủy";
            if (order.OrderStatus.OrderStatusName == "Đang chờ")
            {
                var cancelledStatus = await orderStatuses.GetByNameAsync("Đã hủy");
                order.OrderStatusId = cancelledStatus.OrderStatusId;
                order.OrderStatus = cancelledStatus;
                order.UpdatedAt = DateTime.UtcNow;
            }

            await orders.SaveChangesAsync();
            await notifications.CreateAsync(
                order.CustomerUserId,
                "Thanh toán chưa hoàn tất",
                $"Thanh toán cho đơn #{orderId} đã bị hủy hoặc hết hạn.",
                "payment");
        }

        return new PaymentStatusDto(orderId, payment.PaymentStatus, order.OrderStatus.OrderStatusName, false);
    }
}
