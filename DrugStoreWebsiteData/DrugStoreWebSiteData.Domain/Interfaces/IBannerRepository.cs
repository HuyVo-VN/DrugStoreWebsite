using DrugStoreWebSiteData.Domain.Entities;

namespace DrugStoreWebSiteData.Domain.Interfaces;

public interface IBannerRepository
{
    Task<IEnumerable<Banner>> GetActiveBannersAsync();
    Task<IEnumerable<Banner>> GetAllBannersAsync();
    Task<Banner?> GetByIdAsync(Guid id);
    Task AddAsync(Banner banner);
    void Update(Banner banner);
    Task<bool> DeleteAsync(Guid id);
}