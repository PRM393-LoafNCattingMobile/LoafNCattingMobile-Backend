using LoafNCatting.Data.Models;

namespace LoafNCatting.Data.Interfaces;

public interface IPaymentMethodRepository : IGenericRepository<PaymentMethod>
{
    Task<PaymentMethod> GetByNameOrDefaultAsync(string name);
}

