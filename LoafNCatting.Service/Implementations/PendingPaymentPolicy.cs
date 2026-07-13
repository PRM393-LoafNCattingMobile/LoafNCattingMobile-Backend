using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using Microsoft.Extensions.Configuration;

namespace LoafNCatting.Service.Implementations;

internal static class PendingPaymentPolicy
{
    public const string PendingPaymentStatus = "Đang chờ thanh toán";
    public const string PaidPaymentStatus = "Đã thanh toán";
    public const string CancelledPaymentStatus = "Đã hủy";
    public const string PendingOrderStatus = "Đang chờ";
    public const string CancelledOrderStatus = "Đã hủy";

    private const int DefaultExpirySeconds = 30;

    public static TimeSpan GetExpiry(IConfiguration? configuration)
    {
        if (int.TryParse(configuration?["Payments:PendingPaymentExpirySeconds"], out var seconds) &&
            seconds > 0)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        if (int.TryParse(configuration?["Payments:PendingPaymentExpiryMinutes"], out var minutes) &&
            minutes > 0)
        {
            return TimeSpan.FromMinutes(minutes);
        }

        return TimeSpan.FromSeconds(DefaultExpirySeconds);
    }

    public static bool IsExpired(Order order, IConfiguration? configuration, DateTime? utcNow = null)
    {
        var startedAt = order.OrderDate != default ? order.OrderDate : order.CreatedAt;
        if (startedAt == default)
        {
            return false;
        }

        var startedAtUtc = startedAt.Kind == DateTimeKind.Local
            ? startedAt.ToUniversalTime()
            : startedAt;

        return (utcNow ?? DateTime.UtcNow) - startedAtUtc >= GetExpiry(configuration);
    }

    public static async Task<bool> ExpireIfNeededAsync(
        Order order,
        IOrderStatusRepository orderStatuses,
        IConfiguration? configuration)
    {
        var payment = order.Payments.FirstOrDefault();
        if (payment?.PaymentStatus != PendingPaymentStatus ||
            order.OrderStatus.OrderStatusName != PendingOrderStatus ||
            !IsExpired(order, configuration))
        {
            return false;
        }

        await CancelPendingOrderAsync(order, payment, orderStatuses);
        return true;
    }

    public static async Task CancelPendingOrderAsync(
        Order order,
        Payment payment,
        IOrderStatusRepository orderStatuses)
    {
        payment.PaymentStatus = CancelledPaymentStatus;
        var cancelledStatus = await orderStatuses.GetByNameAsync(CancelledOrderStatus);
        order.OrderStatusId = cancelledStatus.OrderStatusId;
        order.OrderStatus = cancelledStatus;
        order.UpdatedAt = DateTime.UtcNow;
        RestoreReservedStock(order);
    }

    public static void RestoreReservedStock(Order order)
    {
        foreach (var detail in order.OrderDetails)
        {
            if (detail.Product is null)
            {
                continue;
            }

            detail.Product.UnitInStock += detail.Quantity;
            detail.Product.IsAvailable = true;
            detail.Product.UpdatedAt = DateTime.UtcNow;
        }
    }
}
