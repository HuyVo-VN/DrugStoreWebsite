using DrugStoreWebSiteData.Domain.Entities;
using DrugStoreWebSiteData.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using DrugStoreWebSiteData.Application.Common;
using Microsoft.EntityFrameworkCore;
using DrugStoreWebSiteData.Domain.Interfaces;

namespace DrugStoreWebSiteData.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly DrugStoreDbContext _context;
    public CategoryRepository() { }

    public CategoryRepository(DrugStoreDbContext context, ILogger<CategoryRepository> logger)
    {
        _context = context;
    }

    public async Task<Category?> GetByIdAsync(Guid id)
    {
        return await _context.Categories.FindAsync(id);
    }
    
    public async Task<Category?> GetByNameAsync(string name)
    {
        return await _context.Categories.FirstOrDefaultAsync(c => c.Name == name);
    }

    public async Task AddAsync(Category category)
    {
        await _context.Categories.AddAsync(category);
    }

    public async Task<List<Category>> GetAllAsync()
    {
        return await _context.Categories.ToListAsync();
    }

}