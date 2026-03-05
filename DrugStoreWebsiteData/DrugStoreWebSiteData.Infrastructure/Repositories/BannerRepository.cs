using DrugStoreWebSiteData.Domain.Entities;
using DrugStoreWebSiteData.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using DrugStoreWebSiteData.Domain.Interfaces;

namespace DrugStoreWebSite.Infrastructure.Repositories;

public class BannerRepository : IBannerRepository
{
    private readonly DrugStoreDbContext _context;

    public BannerRepository(DrugStoreDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Banner>> GetActiveBannersAsync()
    {
        return await _context.Banners
            .Where(b => b.IsActive)
            .OrderBy(b => b.DisplayOrder)
            .ToListAsync();
    }

    public async Task<IEnumerable<Banner>> GetAllBannersAsync()
    {
        return await _context.Banners.OrderBy(b => b.DisplayOrder).ToListAsync();
    }

    public async Task<Banner?> GetByIdAsync(Guid id)
    {
        return await _context.Banners.FindAsync(id);
    }

    public async Task AddAsync(Banner banner)
    {
        await _context.Banners.AddAsync(banner);
    }

    public void Update(Banner banner)
    {
        _context.Banners.Update(banner);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var banner = await _context.Banners.FindAsync(id);
        if (banner == null) return false;

        _context.Banners.Remove(banner);
        return true;
    }
}