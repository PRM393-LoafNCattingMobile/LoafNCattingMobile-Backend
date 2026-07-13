using LoafNCatting.Service.Interfaces;
using Net.payOS;
using Net.payOS.Types;

namespace LoafNCatting.Service.Implementations;

public class PayOsClient(PayOS payOS) : IPayOsClient
{
    public Task<CreatePaymentResult> CreatePaymentLinkAsync(PaymentData paymentData) =>
        payOS.createPaymentLink(paymentData);

    public Task<PaymentLinkInformation> GetPaymentLinkInformationAsync(long orderCode) =>
        payOS.getPaymentLinkInformation(orderCode);
}

