namespace DrugStoreWebSiteData.Domain.Interfaces;

public interface IDashboardRepository
{
    // --- PRODUCT ---
    Task<Dictionary<string, int>> GetProductStockAsync(int top = 10);
    Task<Dictionary<string, int>> GetTopSellingProductsAsync(int top = 5, int? year = null, int? month = null);

    // --- CATEGORY ---
    Task<Dictionary<string, int>> GetProductsPerCategoryAsync();
    Task<Dictionary<string, decimal>> GetTopCategorySellingAsync(int top = 5, int? year = null, int? month = null);

    // --- ORDER ---
    Task<Dictionary<string, decimal>> GetRevenueByTimeAsync(int? year = null, int? month = null);
    Task<Dictionary<string, int>> GetOrdersByTimeAsync(int? year = null, int? month = null);
    Task<Dictionary<string, int>> GetOrderStatusAsync();
}