using DrugStoreWebSiteData.Domain.Entities;

namespace DrugStoreWebSiteData.Domain.Interfaces;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id);
    Task<List<Category>> GetAllAsync();
    Task<(List<Category> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);
    Task AddAsync(Category category);
    Task<bool> DeleteAsync(Guid categoryId);
    void Update(Category category);
}