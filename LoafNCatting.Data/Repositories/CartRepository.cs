using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;

namespace LoafNCatting.Data.Repositories;

public class CartRepository(LoafNcattingDbContext context) : GenericRepository<Cart>(context), ICartRepository { }

