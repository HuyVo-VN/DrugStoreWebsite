using DrugStoreWebSiteData.Domain.Entities;
using DrugStoreWebSiteData.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using DrugStoreWebSiteData.Domain.Interfaces;

namespace DrugStoreWebSite.Infrastructure.Repositories;

public class CollectionRepository : ICollectionRepository
{
    private readonly DrugStoreDbContext _context;

    public CollectionRepository(DrugStoreDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Collection>> GetHomepageCollectionsAsync()
    {
        return await _context.Collections
            .Include(c => c.ProductCollections)
                .ThenInclude(pc => pc.Product)
            .Where(c => c.IsActive && c.ShowOnHomePage) 
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();
    }

    public async Task<IEnumerable<Collection>> GetAllCollectionsAdminAsync()
    {
        return await _context.Collections
            .Include(c => c.ProductCollections)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();
    }

    public async Task<Collection?> GetByIdAsync(Guid id)
    {
        return await _context.Collections.FindAsync(id);
    }

    public async Task AddAsync(Collection collection)
    {
        await _context.Collections.AddAsync(collection);
    }

    public void Update(Collection collection)
    {
        _context.Collections.Update(collection);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var collection = await _context.Collections.FindAsync(id);
        if (collection == null) return false;
        _context.Collections.Remove(collection);
        return true;
    }

    public async Task UpdateCollectionProductsAsync(Guid collectionId, List<Guid> productIds)
    {
        var oldLinks = await _context.ProductCollections.Where(pc => pc.CollectionId == collectionId).ToListAsync();
        _context.ProductCollections.RemoveRange(oldLinks);

        if (productIds != null && productIds.Any())
        {
            var newLinks = productIds.Select(pId => new ProductCollection
            {
                CollectionId = collectionId,
                ProductId = pId
            });
            await _context.ProductCollections.AddRangeAsync(newLinks);
        }
    }
}
