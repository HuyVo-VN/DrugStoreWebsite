using DrugStoreWebSiteData.Domain.Entities;

namespace DrugStoreWebSiteData.Domain.Interfaces;

public interface ICollectionRepository
{
    Task<IEnumerable<Collection>> GetHomepageCollectionsAsync();
    Task<IEnumerable<Collection>> GetAllCollectionsAdminAsync();
    Task<Collection?> GetByIdAsync(Guid id);
    Task AddAsync(Collection collection);
    void Update(Collection collection);
    Task<bool> DeleteAsync(Guid id);
    Task UpdateCollectionProductsAsync(Guid collectionId, List<Guid> productIds);
}