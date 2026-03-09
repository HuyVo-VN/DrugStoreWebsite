using DrugStoreWebSiteData.Domain.Entities;
using DrugStoreWebSiteData.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using DrugStoreWebSiteData.Domain.Interfaces;
using DrugStoreWebSiteData.Application.Common;

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

    //public async Task<(IEnumerable<Product> Items, int TotalCount)> GetBestSellersPagedAsync(int pageIndex, int pageSize)
    //{
    //    var query = _context.Products
    //        .Where(p => p.IsActive && p.SoldQuantity > 0)
    //        .OrderByDescending(p => p.SoldQuantity);

    //    int totalCount = await query.CountAsync();
    //    var items = await query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();

    //    return (items, totalCount);
    //}

    //public async Task<(IEnumerable<Product> Items, int TotalCount)> GetSaleProductsPagedAsync(int pageIndex, int pageSize)
    //{
    //    var currentTime = DateTime.UtcNow;

    //    var query = _context.Products
    //        .Where(p => p.IsActive
    //                 && p.DiscountPercent > 0
    //                 && p.DiscountEndDate.HasValue
    //                 && p.DiscountEndDate.Value > currentTime
    //                 && p.SaleSold < p.SaleStock);

    //    int totalCount = await query.CountAsync();

    //    var items = await query
    //        .OrderBy(p => p.DiscountEndDate)
    //        .Skip((pageIndex - 1) * pageSize)
    //        .Take(pageSize)
    //        .ToListAsync();

    //    return (items, totalCount);
    //}

    public async Task<(IEnumerable<Product> Items, int TotalCount)> GetSaleProductsPagedAsync(int pageIndex, int pageSize)
    {
        var currentTime = DateTime.UtcNow;

        var query = _context.Products
            .Include(p => p.Category) // Phải có dòng này để lúc map DTO không bị lỗi Null
            .Where(p => p.IsActive
                     && p.DiscountPercent > 0
                     && p.DiscountEndDate > currentTime // Bỏ .HasValue và .Value đi
                     && p.SaleSold < p.SaleStock);

        int totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.DiscountEndDate)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(IEnumerable<Product> Items, int TotalCount)> GetBestSellersPagedAsync(int pageIndex, int pageSize)
    {
        var query = _context.Products
            .Include(p => p.Category) // Phải có dòng này để lúc map DTO không bị lỗi Null
            .Where(p => p.IsActive && p.SoldQuantity > 0)
            .OrderByDescending(p => p.SoldQuantity);

        int totalCount = await query.CountAsync();

        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
    public async Task<(IEnumerable<Product> Items, int TotalCount)> GetProductsByCollectionPagedAsync(Guid collectionId, int pageIndex, int pageSize)
    {
        var query = _context.Products
            .Where(p => p.IsActive && p.ProductCollections.Any(pc => pc.CollectionId == collectionId))
            .OrderByDescending(p => p.Price);

        int totalCount = await query.CountAsync();

        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

}