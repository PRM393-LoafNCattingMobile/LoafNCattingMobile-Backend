using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Data.Repositories;

public class PaymentMethodRepository(LoafNcattingDbContext context) : GenericRepository<PaymentMethod>(context), IPaymentMethodRepository
{
    public async Task<PaymentMethod> GetByNameOrDefaultAsync(string name)
    {
        return await _context.PaymentMethods.FirstOrDefaultAsync(method => method.MethodName.ToLower() == name.Trim().ToLower())
            ?? await _context.PaymentMethods.FirstAsync();
    }
}

