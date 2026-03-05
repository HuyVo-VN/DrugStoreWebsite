using DrugStoreWebSiteData.Domain.Entities;
using DrugStoreWebSiteData.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using DrugStoreWebSiteData.Domain.Interfaces;

namespace DrugStoreWebSite.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly DrugStoreDbContext _context;

    public ProductRepository(DrugStoreDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _context.Products.FindAsync(id);
    }

    public async Task AddAsync(Product product)
    {
        await _context.Products.AddAsync(product);
    }

    public async Task<Boolean> DeleteAsync(Guid productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product != null)
        {
            _context.Products.Remove(product);
            return true;
        }
        return false;
    }
    public async Task<bool> UpdateStatusAsync(Product product)
    {
        var productResult = await _context.Products.FindAsync(product.Id);
        if (productResult != null)
        {
            productResult.UpdateStatus(product.IsActive, product.UpdatedBy, product.UpdatedAt);
            return true;
        }
        return false;
    }
    public void Update(Product product)
    {
        _context.Products.Update(product);

    }
    public async Task<List<Product>> GetAllAsync()
    {
        return await _context.Products.ToListAsync();
    }
    public async Task<(List<Product> Items, int TotalCount)> SearchByNameAsync(string name, int pageNumber, int pageSize)
    {
        var totalCount = await _context.Products.Where(p => p.Name.ToLower().Contains(name.ToLower())).CountAsync();

        var items = await _context.Products.Where(p => p.Name.ToLower()
        .Contains(name.ToLower()))
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
        return (items, totalCount);
    }
    public async Task<(List<Product> Items, int TotalCount)> FilterByCategoryAsync(Guid categoryId, int pageNumber, int pageSize)
    {
        var categoryExists = await _context.Categories.AnyAsync(c => c.Id == categoryId);

        if (!categoryExists)
        {
            return await GetPagedAsync(pageNumber, pageSize); // return all products
        }

        // if exist, filter by category
        var query = _context.Products
            .Include(p => p.Category)
            .Where(p => p.CategoryId == categoryId);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);

    }


    public async Task<(List<Product> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize)
    {
        //total count
        var totalCount = await _context.Products.CountAsync();

        //skip vs take
        var items = await _context.Products
            .Include(p => p.Category)
            .OrderByDescending(p => p.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<IEnumerable<Product>> GetSaleProductsAsync(int limit = 10)
    {

        var now = DateTime.UtcNow;

        return await _context.Products
            .Where(p => p.DiscountPercent > 0 
            && p.IsActive == true
            && p.DiscountEndDate != null
            && p.DiscountEndDate > now
            && p.SaleSold < p.SaleStock)
            .OrderByDescending(p => p.DiscountPercent)
            .OrderBy(p => p.DiscountEndDate)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetBestSellerProductsAsync(int limit = 10)
    {
        return await _context.Products
            .Where(p => p.IsActive && p.SoldQuantity > 0) 
            .OrderByDescending(p => p.SoldQuantity)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetProductsByCollectionNameAsync(string collectionName, int take)
    {
        return await _context.ProductCollections
            .Include(pc => pc.Product)
            .Include(pc => pc.Collection)
            .Where(pc => pc.Collection.Name.Contains(collectionName)
                      && pc.Collection.IsActive
                      && pc.Product.IsActive)
            .Select(pc => pc.Product)
            .Take(take)
            .ToListAsync();
    }

}