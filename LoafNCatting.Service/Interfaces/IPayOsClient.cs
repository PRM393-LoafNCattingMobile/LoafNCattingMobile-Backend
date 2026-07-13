using Net.payOS.Types;

namespace LoafNCatting.Service.Interfaces;

public interface IPayOsClient
{
    Task<CreatePaymentResult> CreatePaymentLinkAsync(PaymentData paymentData);
    Task<PaymentLinkInformation> GetPaymentLinkInformationAsync(long orderCode);
}

