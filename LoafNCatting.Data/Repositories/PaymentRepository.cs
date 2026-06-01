using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;

namespace LoafNCatting.Data.Repositories;

public class PaymentRepository(LoafNcattingDbContext context) : GenericRepository<Payment>(context), IPaymentRepository { }

