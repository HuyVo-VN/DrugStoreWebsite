using DrugStoreWebSiteData.Infrastructure.Persistence;
using DrugStoreWebSiteData.Domain.Entities;
using DrugStoreWebSiteData.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DrugStoreWebSiteData.Infrastructure.Repositories;

public class CartRepository : ICartRepository
{
    private readonly DrugStoreDbContext _context;

    public CartRepository(DrugStoreDbContext context)
    {
        _context = context;
    }

    public async Task<Cart?> GetCartByUserIdAsync(Guid userId)
    {
        return await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task AddAsync(Cart cart)
    {
        await _context.Carts.AddAsync(cart);
    }
    public async Task AddItemAsync(CartItem item)
    {
        await _context.CartItems.AddAsync(item);
    }
    public async Task<bool> RemoveItemAsync(Guid itemId)
    {
        var item = await _context.CartItems.FindAsync(itemId);
        if (item != null)
        {
            _context.CartItems.Remove(item);
            return true;
        }
        return false;
    }
}