using DrugStoreWebSiteData.Domain.Entities;
using DrugStoreWebSiteData.Domain.Enums;

namespace DrugStoreWebSiteData.Domain.Interfaces;

public interface IOrderRepository
{
    Task<List<Order>> GetAllAsync();
    Task AddAsync(Order order);
    Task AddOrderItemAsync(OrderItem orderItem);
    Task<string?> GetAddressAsync(Guid userId);
    Task<(List<Order> Items, int TotalCount)> FilterByUserAndStatusAsync(Guid userId, OrderStatus status, int pageNumber, int pageSize);
    Task<(List<Order> Items, int TotalCount)> GetByUserIdAsync(Guid userId, int pageNumber, int pageSize);
    Task<Order?> GetByOrderIdAsync(Guid orderId);
    Task<bool> UpdateStatusAsync(Order order);
    Task<Order?> GetOrderItemsByOrderIdAsync(Guid orderId);
    Task<bool> DeleteAsync(Guid orderId);
}