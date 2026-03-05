using System.Security.Claims;
using DrugStoreWebSiteData.Application.Common;
using DrugStoreWebSiteData.Application.DTOs;
using DrugStoreWebSiteData.Application.DTOs.Request;
using DrugStoreWebSiteData.Application.Interfaces;
using DrugStoreWebSiteData.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DrugStoreWebSiteData.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly ILogger<CartController> _logger;

    public CartController(ICartService cartService,
                        ILogger<CartController> logger)
    {
        _cartService = cartService;
        _logger = logger;
    }

    // helper get UserId from accesstoken
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

    [HttpGet("get-cart")]
    [ProducesResponseType(typeof(ResponseModel<CartResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel<CartResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCart()
    {

        var responseResult = new ResponseModel<CartResponseDto>();
        try
        {
            var cartResult = await _cartService.GetCartAsync(GetUserId());
            if (cartResult.IsFailure)
            {
                _logger.LogError("Failed to retrieve cart: {Error}", cartResult.Error);
                responseResult.Status = 400;
                responseResult.Message = cartResult.Error;
                return BadRequest(responseResult);
            }
            responseResult.Status = 200;
            responseResult.Data = cartResult.Value;
            responseResult.Message = "Cart retrieved successfully";
            return Ok(responseResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving cart");
            responseResult.Status = 400;
            responseResult.Message = ex.Message;
            return BadRequest(responseResult);
        }
    }

    [HttpPost("add")]
    [ProducesResponseType(typeof(ResponseModel<CartItemResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel<CartItemResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequestDto request)
    {
        var responseResult = new ResponseModel<CartItemResponseDto>();
        try
        {
            if (!ModelState.IsValid)
            {
                responseResult.Status = 400;
                responseResult.Message = "Invalid request data.";
                return BadRequest(responseResult);
            }
            // get UserId from accesstoken
            var userId = GetUserId();

            var serviceResult = await _cartService.AddToCartAsync(userId, request);

            if (serviceResult.IsFailure)
            {
                responseResult.Status = 400;
                responseResult.Message = serviceResult.Error;
                return BadRequest(responseResult);
            }

            responseResult.Status = 200;
            responseResult.Data = serviceResult.Value;
            responseResult.Message = "Product added to cart successfully";

            return Ok(responseResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to cart");
            responseResult.Status = 400;
            responseResult.Message = ex.Message;
            return BadRequest(responseResult);
        }
    }

    [ProducesResponseType(typeof(CartItemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpDelete("remove")]
    public async Task<IActionResult> RemoveFromCart([FromBody] RemoveFromCartRequestDto requestDto)
    {
        var result = new ResponseModel<string>();
        try
        {
            var cartItemResult = await _cartService.RemoveAsync(requestDto.ItemId);
            if (cartItemResult.IsFailure)
                return BadRequest(cartItemResult.Error);
            result.Status = 200;
            result.Message = cartItemResult.Value;
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(Result<string>.Failure($"An unexpected error occurred while removing item from cart: {ex.Message}"));
        }
    }

    [HttpPut("update-quantity")]
    [ProducesResponseType(typeof(ResponseModel<CartItemResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel<CartItemResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateQuantity([FromBody] UpdateCartItemRequestDto request)
    {
        var responseResult = new ResponseModel<CartItemResponseDto>();
        try
        {
            var userId = GetUserId();
            var serviceResult = await _cartService.UpdateQuantityAsync(userId, request);

            if (serviceResult.IsFailure)
            {
                responseResult.Status = 400;
                responseResult.Message = serviceResult.Error;
                return BadRequest(responseResult);
            }

            responseResult.Status = 200;
            responseResult.Data = serviceResult.Value;
            responseResult.Message = "Quantity updated successfully";
            return Ok(responseResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating quantity");
            return BadRequest(Result<CartItemResponseDto>.Failure($"An unexpected error occurred while update item from cart: {ex.Message}"));
        }
    }
}