using DrugStoreWebSiteData.Application.Common;
using DrugStoreWebSiteData.Application.DTOs;
using DrugStoreWebSiteData.Application.DTOs.Request;

namespace DrugStoreWebSiteData.Application.Interfaces;

public interface ICartService
{
    Task<Result<CartResponseDto>> GetCartAsync(Guid userId);
    Task<Result<CartItemResponseDto>> AddToCartAsync(Guid userId, AddToCartRequestDto request);
    Task<Result<string>> RemoveAsync(Guid cartItemId);
    Task<Result<CartItemResponseDto>> UpdateQuantityAsync(Guid userId, UpdateCartItemRequestDto request);
    }