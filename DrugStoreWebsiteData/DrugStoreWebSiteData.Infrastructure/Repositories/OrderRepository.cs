using DrugStoreWebSiteData.Domain.Entities;
using DrugStoreWebSiteData.Domain.Enums;
using DrugStoreWebSiteData.Domain.Interfaces;
using DrugStoreWebSiteData.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DrugStoreWebSiteData.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly DrugStoreDbContext _context;

    public OrderRepository(DrugStoreDbContext context)
    {
        _context = context;
    }

    public async Task<List<Order>> GetAllAsync()
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<(List<Order> Items, int TotalCount)> GetByUserIdAsync(Guid userId, int pageNumber, int pageSize)
    {
        var query = _context.Orders
            .Where(o => o.UserId == userId);

        var totalCount = await query.CountAsync();

        var items = await query
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.OrderDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
    public async Task<Order?> GetOrderItemsByOrderIdAsync(Guid orderId)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }
    public async Task AddAsync(Order order)
    {
        await _context.Orders.AddAsync(order);
    }
    public async Task AddOrderItemAsync(OrderItem orderItem)
    {
        await _context.OrderItems.AddAsync(orderItem);
    }

    public async Task<string?> GetAddressAsync(Guid userId)
    {
        var latestOrder = await _context.Orders
            .Where(o => o.UserId == userId && o.ShippingAddress != null)
            .OrderByDescending(o => o.OrderDate)
            .FirstOrDefaultAsync();

        return latestOrder?.ShippingAddress;
    }

    public async Task<(List<Order> Items, int TotalCount)> FilterByUserAndStatusAsync(Guid userId, OrderStatus status, int pageNumber, int pageSize)
    {
        var query = _context.Orders
            .Where(o => o.UserId == userId && o.Status == status);

        var totalCount = await query.CountAsync();

        var items = await query
        .Include(o => o.OrderItems)
        .ThenInclude(oi => oi.Product)
        .OrderByDescending(o => o.OrderDate)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

        return (items, totalCount);
    }
     public async Task<Order?> GetByOrderIdAsync(Guid orderId)
    {
        return await _context.Orders.FindAsync(orderId);
    }
      public async Task<bool> UpdateStatusAsync(Order order)
    {
        var orderResult = await _context.Orders.FindAsync(order.Id);
        if (orderResult != null)
        {
            orderResult.UpdateStatus(order.Status);
            return true;
        }
        return false;
    }
    public async Task<bool> DeleteAsync(Guid orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order != null)
        {
            _context.Orders.Remove(order);
            return true;
        }
        return false;
    }

}