using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;

namespace LoafNCatting.Data.Repositories;

public class CartItemRepository(LoafNcattingDbContext context) : GenericRepository<CartItem>(context), ICartItemRepository { }

