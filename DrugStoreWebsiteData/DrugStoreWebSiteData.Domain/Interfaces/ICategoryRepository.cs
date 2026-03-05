using DrugStoreWebSiteData.Domain.Entities;

namespace DrugStoreWebSiteData.Domain.Interfaces;

public interface ICategoryRepository
{
    //Only return the entity or null if not found
    Task<Category?> GetByIdAsync(Guid id);
    Task<Category?> GetByNameAsync(string name);
    Task<List<Category>> GetAllAsync();
    Task AddAsync(Category category);
}