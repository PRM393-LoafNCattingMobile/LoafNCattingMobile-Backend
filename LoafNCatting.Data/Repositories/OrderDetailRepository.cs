using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;

namespace LoafNCatting.Data.Repositories;

public class OrderDetailRepository(LoafNcattingDbContext context) : GenericRepository<OrderDetail>(context), IOrderDetailRepository { }

