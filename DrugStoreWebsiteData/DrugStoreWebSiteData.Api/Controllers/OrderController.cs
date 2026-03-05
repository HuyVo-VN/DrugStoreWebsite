using System.Security.Claims;
using DrugStoreWebSiteData.Application.Common;
using DrugStoreWebSiteData.Application.DTOs;
using DrugStoreWebSiteData.Application.DTOs.Request;
using DrugStoreWebSiteData.Application.Interfaces;
using DrugStoreWebSiteData.Domain.Enums;
using DrugStoreWebSiteData.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DrugStoreWebSiteData.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrderController> _logger;

    public OrderController(IOrderService orderService, ILogger<OrderController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null)
        {
            _logger.LogWarning("User ID claim not found in token.");
            return Guid.Empty;
        }
        else
        {
            return Guid.Parse(userIdClaim!.Value);
        }
    }

    [HttpGet("all")]
    [Authorize(Roles = RoleConstants.ManagerRoles)]
    [ProducesResponseType(typeof(ResponseModel<OrderResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel<OrderResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAllOrders()
    {
        var result = new ResponseModel<List<OrderResponseDto>>();
        try
        {
            var serviceResult = await _orderService.GetAllOrdersAsync();
            if (serviceResult.IsFailure)
            {
                _logger.LogError("Failed to retrieve order: {Error}", serviceResult.Error);
                result.Status = 400;
                result.Message = serviceResult.Error;
                return BadRequest(result);
            }

            result.Status = 200;
            result.Data = serviceResult.Value;
            result.Message = "Order retrieved successfully";
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving order");
            return BadRequest(Result<OrderResponseDto>.Failure($"An unexpected error occurred while get all order: {ex.Message}"));
        }
    }

    [HttpGet("customer-orders")]
    [ProducesResponseType(typeof(ResponseModel<OrderResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel<OrderResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCustomerOrders([FromQuery] PaginationRequestDto paginationQuery)
    {
        var result = new ResponseModel<PagedResult<OrderResponseDto>>();
        try
        {
            var userId = GetUserId();
            var serviceResult = await _orderService.GetOrdersByUserIdAsync(userId, paginationQuery);

            if (serviceResult.IsFailure)
            {
                _logger.LogError("Failed to retrieve order: {Error}", serviceResult.Error);
                result.Status = 400;
                result.Message = serviceResult.Error;
                return BadRequest(result);
            }

            result.Status = 200;
            result.Data = serviceResult.Value;
            result.Message = "Success";
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving order");
            return BadRequest(Result<OrderResponseDto>.Failure($"An unexpected error occurred while get my order: {ex.Message}"));

        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ResponseModel<OrderResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel<OrderResponseDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseModel<OrderResponseDto>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOrderDetails(Guid id)
    {
        var result = new ResponseModel<OrderResponseDto>();
        try
        {
            var serviceResult = await _orderService.GetOrderItemsByOrderIdAsync(id);

            if (serviceResult.IsFailure)
            {
                result.Status = 404;
                result.Message = serviceResult.Error;
                return NotFound(result);
            }

            var orderDto = serviceResult.Value;

            // check authorized
            var currentUserId = GetUserId();

            // get role from token
            var isManager = User.IsInRole(RoleConstants.Admin) || User.IsInRole(RoleConstants.Staff);

            if (!isManager && orderDto.UserId != currentUserId)
            {
                result.Status = 403;
                result.Message = "You are not authorized to view this order.";
                return StatusCode(403, result);
            }

            result.Status = 200;
            result.Data = orderDto;
            result.Message = "Success";
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving order");
            return BadRequest(Result<OrderResponseDto>.Failure($"An unexpected error occurred while get order: {ex.Message}"));
        }
    }

    [HttpPost("create-order")]
    [ProducesResponseType(typeof(ResponseModel<OrderResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel<OrderResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto createOrderRequestDto)
    {
        var responseResult = new ResponseModel<OrderResponseDto>();
        try
        {
            if (!ModelState.IsValid)
            {
                responseResult.Message = "Invalid request data.";
                return BadRequest(responseResult);
            }

            var userId = GetUserId();

            var serviceResult = await _orderService.CreateOrderAsync(userId, createOrderRequestDto);

            if (serviceResult.IsFailure)
            {
                responseResult.Message = serviceResult.Error;
                return BadRequest(responseResult);
            }

            responseResult.Data = serviceResult.Value;
            responseResult.Message = "Create new order successfully";

            return Ok(responseResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating new order");
            responseResult.Message = ex.Message;
            return BadRequest(responseResult);
        }
    }

    [ProducesResponseType(typeof(List<OrderResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpGet("get-customer-address")]
    public async Task<IActionResult> GetCustomerAddress()
    {
        try
        {
            var userId = GetUserId();

            var result = await _orderService.GetLatestAddressOfCustomerAsync(userId);

            if (result.IsFailure)
            {
                _logger.LogError($"Failed to get customer address: {result.Error}");
                return BadRequest(result.Error);
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error occurred while geting customer address: {ex.Message}");
            return BadRequest(Result<string>.Failure($"An unexpected error occurred while geting customer address: {ex.Message}"));
        }
    }

    [ProducesResponseType(typeof(List<OrderResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpGet("filter-customer-orders")]
    public async Task<IActionResult> FilterOrders([FromQuery] OrderStatus status, [FromQuery] PaginationRequestDto paginationQuery)
    {
        try
        {
            var userId = GetUserId();

            var filterResult = await _orderService.FilterOrdersAsync(userId, status, paginationQuery);

            if (filterResult.IsFailure)
            {
                _logger.LogError($"Failed to filter products: {filterResult.Error}");
                return BadRequest(filterResult.Error);
            }
            return Ok(filterResult);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error occurred while filtering orders: {ex.Message}");
            return BadRequest(Result<string>.Failure($"An unexpected error occurred while filtering orders: {ex.Message}"));
        }
    }

    [Authorize(Roles = RoleConstants.ManagerRoles)]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpDelete("delete-order")]
    public async Task<IActionResult> DeleteOrder([FromBody] DeleteOrderRequestDto requestDto)
    {
        try
        {
            var orderResult = await _orderService.DeleteOrderAsync(requestDto.OrderId);
            if (orderResult.IsFailure)
                return BadRequest(orderResult.Error);
            return Ok(orderResult.Value);
        }
        catch (Exception ex)
        {
            return BadRequest(Result<string>.Failure($"An unexpected error occurred while deleting order: {ex.Message}"));
        }
    }

    [Authorize(Roles = RoleConstants.ManagerRoles)]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpPatch("update-status-order")]
    public async Task<IActionResult> UpadateStatusOrder([FromBody] UpdateOrderStatusRequestDto requestDto)
    {
        try
        {
            var orderResult = await _orderService.UpdateStatusAsync(requestDto);
            if (orderResult.IsFailure)
                return BadRequest(orderResult.Error);

            return Ok(orderResult);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error occurred while updating status of order: {ex.Message}");
            return BadRequest(Result<string>.Failure($"An unexpected error occurred while updating status order: {ex.Message}"));
        }
    }
}