using DrugStoreWebSiteData.Domain.Entities;

namespace DrugStoreWebSiteData.Domain.Interfaces;

public interface ICartRepository
{
    Task<Cart?> GetCartByUserIdAsync(Guid userId);
    Task AddAsync(Cart cart);
    Task AddItemAsync(CartItem item);
    Task<bool> RemoveItemAsync(Guid itemId);
}