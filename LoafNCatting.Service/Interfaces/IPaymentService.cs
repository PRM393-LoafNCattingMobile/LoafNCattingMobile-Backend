using LoafNCatting.Service.DTOs;

namespace LoafNCatting.Service.Interfaces;

public interface IPaymentService
{
    Task<PaymentLinkDto?> CreatePaymentLinkAsync(int orderId, int userId);
    Task<PaymentStatusDto?> GetPaymentStatusAsync(int orderId, int userId);
}
