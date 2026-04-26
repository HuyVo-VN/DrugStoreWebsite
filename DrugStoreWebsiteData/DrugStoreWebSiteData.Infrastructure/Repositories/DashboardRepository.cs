using DrugStoreWebSiteData.Domain.Interfaces;
using DrugStoreWebSiteData.Infrastructure.Persistence; // Trỏ đúng DbContext của sếp
using Microsoft.EntityFrameworkCore;
using DrugStoreWebSiteData.Domain.Enums;

namespace DrugStoreWebSiteData.Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly DrugStoreDbContext _context;

    public DashboardRepository(DrugStoreDbContext context)
    {
        _context = context;
    }

    // ==========================================
    // 1. PRODUCT
    // ==========================================
    public async Task<Dictionary<string, int>> GetProductStockAsync(int top = 10)
    {
        return await _context.Products
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.Stock)
            .Take(top)
            .ToDictionaryAsync(p => p.Name, p => p.Stock);
    }

    public async Task<Dictionary<string, int>> GetTopSellingProductsAsync(int top = 5, int? year = null, int? month = null)
    {
        var query = _context.OrderItems
            .Join(_context.Orders, oi => oi.OrderId, o => o.Id, (oi, o) => new { oi, o })
            .Where(x => x.o.Status == OrderStatus.Completed || x.o.Status == OrderStatus.Paid);

        if (year.HasValue && year > 0) query = query.Where(x => x.o.OrderDate.Year == year.Value);
        if (month.HasValue && month > 0) query = query.Where(x => x.o.OrderDate.Month == month.Value);

        return await query
            .GroupBy(x => x.oi.Product.Name)
            .Select(g => new { ProductName = g.Key, TotalSold = g.Sum(x => x.oi.Quantity) })
            .OrderByDescending(x => x.TotalSold)
            .Take(top)
            .ToDictionaryAsync(x => x.ProductName, x => x.TotalSold);
    }

    // ==========================================
    // 2. CATEGORY
    // ==========================================
    public async Task<Dictionary<string, int>> GetProductsPerCategoryAsync()
    {
        // Join 2 bảng Products và Categories
        var data = await _context.Products
            .Join(_context.Categories, p => p.CategoryId, c => c.Id, (p, c) => new { p, c })
            .GroupBy(x => x.c.Name)
            .Select(g => new { CatName = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CatName, x => x.Count);

        return data;
    }

    public async Task<Dictionary<string, decimal>> GetTopCategorySellingAsync(int top = 5, int? year = null, int? month = null)
    {
        var query = _context.OrderItems
            .Join(_context.Orders, oi => oi.OrderId, o => o.Id, (oi, o) => new { oi, o })
            .Join(_context.Products, x => x.oi.ProductId, p => p.Id, (x, p) => new { x.oi, x.o, p })
            .Join(_context.Categories, x => x.p.CategoryId, c => c.Id, (x, c) => new { x.oi, x.o, x.p, c })
            .Where(x => x.o.Status == OrderStatus.Completed || x.o.Status == OrderStatus.Paid);

        if (year.HasValue && year > 0) query = query.Where(x => x.o.OrderDate.Year == year.Value);
        if (month.HasValue && month > 0) query = query.Where(x => x.o.OrderDate.Month == month.Value);

        return await query
            .GroupBy(x => x.c.Name)
            .Select(g => new { CatName = g.Key, Revenue = g.Sum(x => x.oi.Price * x.oi.Quantity) })
            .OrderByDescending(x => x.Revenue)
            .Take(top)
            .ToDictionaryAsync(x => x.CatName, x => x.Revenue);
    }

    // ==========================================
    // 3. ORDER
    // ==========================================
    public async Task<Dictionary<string, decimal>> GetRevenueByTimeAsync(int? year = null, int? month = null)
    {
        var query = _context.Orders.Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Paid);

        if (year.HasValue && year > 0) query = query.Where(o => o.OrderDate.Year == year.Value);
        if (month.HasValue && month > 0) query = query.Where(o => o.OrderDate.Month == month.Value);

        var orders = await query.ToListAsync();
        var dict = new Dictionary<string, decimal>();

        if (year.HasValue && month.HasValue && month > 0)
        {
            // Xem theo tháng -> Chia nhỏ ra từng Ngày
            int days = DateTime.DaysInMonth(year.Value, month.Value);
            for (int i = 1; i <= days; i++) dict[$"Day {i}"] = 0;
            foreach (var g in orders.GroupBy(o => o.OrderDate.Day)) dict[$"Day {g.Key}"] = g.Sum(o => o.TotalAmount);
        }
        else if (year.HasValue && year > 0)
        {
            // Xem theo Năm -> Chia nhỏ ra 12 Tháng
            for (int i = 1; i <= 12; i++) dict[$"Month {i}"] = 0;
            foreach (var g in orders.GroupBy(o => o.OrderDate.Month)) dict[$"Month {g.Key}"] = g.Sum(o => o.TotalAmount);
        }
        else
        {
            // Không chọn gì -> Nhóm theo các Năm hiện có
            var grouped = orders.GroupBy(o => o.OrderDate.Year).OrderBy(g => g.Key);
            foreach (var g in grouped) dict[$"Year {g.Key}"] = g.Sum(o => o.TotalAmount);
        }
        return dict;
    }

    public async Task<Dictionary<string, int>> GetOrdersByTimeAsync(int? year = null, int? month = null)
    {
        var query = _context.Orders.AsQueryable();

        if (year.HasValue && year > 0) query = query.Where(o => o.OrderDate.Year == year.Value);
        if (month.HasValue && month > 0) query = query.Where(o => o.OrderDate.Month == month.Value);

        var orders = await query.ToListAsync();
        var dict = new Dictionary<string, int>();

        if (year.HasValue && month.HasValue && month > 0)
        {
            int days = DateTime.DaysInMonth(year.Value, month.Value);
            for (int i = 1; i <= days; i++) dict[$"Day {i}"] = 0;
            foreach (var g in orders.GroupBy(o => o.OrderDate.Day)) dict[$"Day {g.Key}"] = g.Count();
        }
        else if (year.HasValue && year > 0)
        {
            for (int i = 1; i <= 12; i++) dict[$"Month {i}"] = 0;
            foreach (var g in orders.GroupBy(o => o.OrderDate.Month)) dict[$"Month {g.Key}"] = g.Count();
        }
        else
        {
            var grouped = orders.GroupBy(o => o.OrderDate.Year).OrderBy(g => g.Key);
            foreach (var g in grouped) dict[$"Year {g.Key}"] = g.Count();
        }
        return dict;
    }

    public async Task<Dictionary<string, int>> GetOrderStatusAsync()
    {
        // Gom nhóm theo mã số int của Status
        var statusCounts = await _context.Orders
            .GroupBy(o => o.Status)
            .Select(g => new { StatusCode = g.Key, Count = g.Count() })
            .ToListAsync();

        var dict = new Dictionary<string, int>();

        // Map mã số sang tên Enum của sếp cho đẹp trên biểu đồ
        foreach (var item in statusCounts)
        {
            string statusName = item.StatusCode switch
            {
                OrderStatus.New => "New",
                OrderStatus.Processing => "Processing",
                OrderStatus.Completed => "Completed",
                OrderStatus.Cancelled => "Cancelled",
                OrderStatus.Paid => "Paid",
                _ => "Unknown"
            };
            dict[statusName] = item.Count;
        }

        return dict;
    }
}