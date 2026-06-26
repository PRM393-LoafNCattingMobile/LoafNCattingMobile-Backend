using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Data.Repositories;

public class CartRepository(LoafNcattingDbContext context) : GenericRepository<Cart>(context), ICartRepository
{
    public async Task<Cart?> GetByUserIdWithItemsAsync(int userId)
    {
        return await _context.Carts
            .Include(cart => cart.CartItems)
            .ThenInclude(item => item.Product)
            .ThenInclude(product => product.Category)
            .FirstOrDefaultAsync(cart => cart.UserId == userId);
    }
}

