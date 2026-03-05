using DrugStoreWebSiteData.Domain.Entities;

namespace DrugStoreWebSiteData.Domain.Interfaces;

public interface IProductRepository
{
    //Only return the entity or null if not found
    Task<Product?> GetByIdAsync(Guid id);
    Task<List<Product>> GetAllAsync();
    Task AddAsync(Product product);
    Task<Boolean> DeleteAsync(Guid productId);
    Task<bool> UpdateStatusAsync(Product product);
    void Update(Product product);
    Task<(List<Product> Items, int TotalCount)> SearchByNameAsync(string name, int pageNumber, int pageSize);
    Task<(List<Product> Items, int TotalCount)> FilterByCategoryAsync(Guid categoryId, int pageNumber, int pageSize);
    Task<(List<Product> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);

    Task<IEnumerable<Product>> GetSaleProductsAsync(int limit = 10);
    Task<IEnumerable<Product>> GetBestSellerProductsAsync(int limit = 10);

    Task<IEnumerable<Product>> GetProductsByCollectionNameAsync(string collectionName, int take);
}