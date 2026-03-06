using DrugStoreWebSiteData.Application.Common;
using DrugStoreWebSiteData.Application.DTOs;
using DrugStoreWebSiteData.Application.DTOs.Request;
using DrugStoreWebSiteData.Application.Interfaces;
using DrugStoreWebSiteData.Domain.Entities;
using DrugStoreWebSiteData.Domain.Enums;
using DrugStoreWebSiteData.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace DrugStoreWebSiteData.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<OrderService> _logger;
    private readonly IUnitOfWork _unitOfWork;


    public OrderService(IOrderRepository orderRepository, IProductRepository productRepository, ILogger<OrderService> logger, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _logger = logger;
        _unitOfWork = unitOfWork;

    }

    public async Task<Result<List<OrderResponseDto>>> GetAllOrdersAsync()
    {
        try
        {
            var orders = await _orderRepository.GetAllAsync();
            if (orders == null)
            {
                _logger.LogError("Failed to retrieve orders");
                return Result<List<OrderResponseDto>>.Failure("Order list not found");
            }

            var orderDtos = new List<OrderResponseDto>();
            var dtoHelper = new OrderResponseDto();

            foreach (var order in orders)
            {
                orderDtos.Add(dtoHelper.MapToOrderDto(order));
            }

            _logger.LogInformation("Retrieved all orders successfully.");
            return Result<List<OrderResponseDto>>.Success(orderDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all orders");
            return Result<List<OrderResponseDto>>.Failure($"An error occurred while retrieving orders: {ex.Message}");
        }
    }

    public async Task<Result<PagedResult<OrderResponseDto>>> GetOrdersByUserIdAsync(Guid userId, PaginationRequestDto paginationQuery)
    {
        try
        {
            var pageNumber = paginationQuery.PageNumber;
            var pageSize = paginationQuery.PageSize;

            var (orders, totalCount) = await _orderRepository.GetByUserIdAsync(userId, pageNumber, pageSize);
            if (orders == null)
            {
                _logger.LogError("Failed to retrieve orders");
                return Result<PagedResult<OrderResponseDto>>.Failure("Order list not found");
            }

            var orderDtos = new List<OrderResponseDto>();
            var dtoHelper = new OrderResponseDto();

            foreach (var order in orders)
            {
                orderDtos.Add(dtoHelper.MapToOrderDto(order));
            }
            var pagedResult = new PagedResult<OrderResponseDto>(orderDtos, totalCount, pageNumber, pageSize);

            _logger.LogInformation("Retrieved orders for user {UserId} successfully.", userId);
            return Result<PagedResult<OrderResponseDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders for user {UserId}", userId);
            return Result<PagedResult<OrderResponseDto>>.Failure($"An error occurred while retrieving orders: {ex.Message}");
        }
    }
    public async Task<Result<OrderResponseDto>> GetOrderItemsByOrderIdAsync(Guid orderId)
    {
        try
        {
            var order = await _orderRepository.GetOrderItemsByOrderIdAsync(orderId);
            if (order == null)
            {
                return Result<OrderResponseDto>.Failure("Order not found");
            }

            var mapper = new OrderResponseDto();
            return Result<OrderResponseDto>.Success(mapper.MapToOrderDto(order));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order {OrderId}", orderId);
            return Result<OrderResponseDto>.Failure(ex.Message);
        }
    }

    public async Task<Result<OrderResponseDto>> CreateOrderAsync(Guid userId, CreateOrderRequestDto request)
    {
        try
        {
            var newOrder = new Order(
                userId,
                request.TotalAmount,
                request.ShippingAddress,
                request.PhoneNumber,
                null
            );

            await _orderRepository.AddAsync(newOrder);

            foreach (var item in request.Items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);

                if (product == null)
                    return Result<OrderResponseDto>.Failure("Product not found.");

                if (product.Stock < item.Quantity)
                    return Result<OrderResponseDto>.Failure($"Product {product.Name} out of stock.");

                if (product.Stock < item.Quantity)
                {
                    return Result<OrderResponseDto>.Failure($"Product '{product.Name}' still have {product.Stock}.");
                }
                bool isFlashSaleActive = product.DiscountPercent > 0
                              && product.DiscountEndDate.HasValue
                              && product.DiscountEndDate.Value > DateTime.UtcNow;

                if (isFlashSaleActive)
                {
                    int remainingSaleStock = product.SaleStock - product.SaleSold;

                    if (item.Quantity > remainingSaleStock)
                    {
                        if (remainingSaleStock > 0)
                        {
                            return Result<OrderResponseDto>.Failure($"Product '{product.Name}' still have {remainingSaleStock} Flash Sale price. Please reduce the quantity!");
                        }
                        else
                        {
                            return Result<OrderResponseDto>.Failure($"Product '{product.Name}' Flash Sale is over. Please refresh the page to see the updated prices!");
                        }
                    }
                }

                var orderItem = new OrderItem(
                    newOrder.Id,
                    item.ProductId,
                    product.Name,
                    product.ImageUrl,
                    product.Price,
                    item.Quantity,
                    product
                );
                await _orderRepository.AddOrderItemAsync(orderItem);

                product.DecreaseStock(item.Quantity);
                product.UpdateStockAndSales(item.Quantity);
            }

            await _unitOfWork.SaveChangesAsync();

            var response = new OrderResponseDto().MapToOrderDto(newOrder);

            return Result<OrderResponseDto>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return Result<OrderResponseDto>.Failure(ex.Message);
        }
    }
    public async Task<Result<PagedResult<OrderResponseDto>>> FilterOrdersAsync(Guid userId, OrderStatus status, PaginationRequestDto paginationQuery)
    {
        try
        {
            var pageNumber = paginationQuery.PageNumber;
            var pageSize = paginationQuery.PageSize;

            var (orders, totalCount) = await _orderRepository.FilterByUserAndStatusAsync(userId, status, pageNumber, pageSize);

            var dtos = orders
                .Select(o => new OrderResponseDto().MapToOrderDto(o))
                .ToList();
            var pagedResult = new PagedResult<OrderResponseDto>(dtos, totalCount, pageNumber, pageSize);

            return Result<PagedResult<OrderResponseDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error filtering orders");
            return Result<PagedResult<OrderResponseDto>>.Failure("Failed to filter orders");
        }
    }

    public async Task<Result<string?>> GetLatestAddressOfCustomerAsync(Guid userId)
    {
        try
        {
            var address = await _orderRepository.GetAddressAsync(userId);
            _logger.LogInformation("Retrieved address for user {UserId} successfully.", userId);
            return Result<string?>.Success(address);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving address for user {UserId}", userId);
            return Result<string?>.Failure($"An error occurred while retrieving address: {ex.Message}");
        }
    }
      public async Task<Result<string>> DeleteOrderAsync(Guid orderId)
    {
        try
        {
            var result = await _orderRepository.DeleteAsync(orderId);
            if (result)
            {
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation($"Order with ID: {orderId} deleted successfully.");
                return Result<string>.Success("Order deleted successfully");
            }
            return Result<string>.Failure("Error occurred while deleting order");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting order with ID: {Id}", orderId);
            return Result<string>.Failure($"An error occurred while deleting the order: {ex.Message}");
        }
    }
    public async Task<Result<string>> UpdateStatusAsync(UpdateOrderStatusRequestDto requestDto)
    {
        try
        {
            var ordertDetail = await _orderRepository.GetByOrderIdAsync(requestDto.OrderId);
            if (ordertDetail != null)
            {
                try
                {
                    ordertDetail.UpdateStatus(requestDto.NewStatus);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"An error occurred while updating status order detail: {ex.Message}");
                    return Result<string>.Failure($"An error occurred while updating status order detail: {ex.Message}");
                }

                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation($"Order with ID: {ordertDetail.Id} updated successfully.");
                return Result<string>.Success("Order updated successfully");
            }

            return Result<string>.Failure("Error occurred while geting order to update ");
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred while updating order: {ex.Message}");
            return Result<string>.Failure($"An error occurred while updating the order: {ex.Message}");
        }
    }

}