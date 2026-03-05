using DrugStoreWebSiteData.Application.Common;
using DrugStoreWebSiteData.Application.DTOs;
using DrugStoreWebSiteData.Application.DTOs.Request;
using DrugStoreWebSiteData.Domain.Enums;

namespace DrugStoreWebSiteData.Application.Interfaces;

public interface IOrderService
{
    Task<Result<List<OrderResponseDto>>> GetAllOrdersAsync();
    Task<Result<OrderResponseDto>> CreateOrderAsync(Guid userId, CreateOrderRequestDto request);
    Task<Result<string?>> GetLatestAddressOfCustomerAsync(Guid userId);
    Task<Result<PagedResult<OrderResponseDto>>> FilterOrdersAsync(Guid userId, OrderStatus status, PaginationRequestDto paginationQuery);
    Task<Result<PagedResult<OrderResponseDto>>> GetOrdersByUserIdAsync(Guid userId, PaginationRequestDto paginationQuery);
    Task<Result<string>> UpdateStatusAsync(UpdateOrderStatusRequestDto requestDto);
    Task<Result<OrderResponseDto>> GetOrderItemsByOrderIdAsync(Guid orderId);
    Task<Result<string>> DeleteOrderAsync(Guid orderId);
}