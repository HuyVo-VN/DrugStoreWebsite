using DrugStoreWebSiteData.Application.Common;
using DrugStoreWebSiteData.Application.DTOs;
using DrugStoreWebSiteData.Application.DTOs.Request;
using DrugStoreWebSiteData.Application.Interfaces;
using DrugStoreWebSiteData.Domain.Entities;
using DrugStoreWebSiteData.Domain.Interfaces;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;

namespace DrugStoreWebSiteData.Application.Services;

public class CartService : ICartService
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<CartService> _logger;
    private readonly IUnitOfWork _unitOfWork;


    public CartService(ICartRepository cartRepository,
        ILogger<CartService> logger,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _cartRepository = cartRepository;
        _logger = logger;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CartResponseDto>> GetCartAsync(Guid userId)
    {
        try
        {
            var cart = await _cartRepository.GetCartByUserIdAsync(userId);

            // if there is not any cart, return null
            if (cart == null)
            {
                return Result<CartResponseDto>.Success(new CartResponseDto { UserId = userId });
            }

            // Map Entity -> DTO
            var dtoHelper = new CartResponseDto();
            var cartDto = dtoHelper.mapToCartDto(cart);

            return Result<CartResponseDto>.Success(cartDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cart");
            return Result<CartResponseDto>.Failure(ex.Message);
        }
    }

    public async Task<Result<CartItemResponseDto>> AddToCartAsync(Guid userId, AddToCartRequestDto request)
    {
        try
        {
            // check if product exist
            var product = await _productRepository.GetByIdAsync(request.ProductId);
            if (product == null)
            {
                return Result<CartItemResponseDto>.Failure("Product not found.");
            }

            // check stock < 0
            if (product.Stock < 0)
            {
                return Result<CartItemResponseDto>.Failure("Product was out of Stock.");
            }
            // check stock < quantity
            if (product.Stock < request.Quantity)
            {
                return Result<CartItemResponseDto>.Failure("Quantity reached product limit.");
            }

            // get user cart
            var cart = await _cartRepository.GetCartByUserIdAsync(userId);
            CartItem? currentItem = null;

            // if dont have cart -> create
            if (cart == null)
            {
                cart = new Cart(userId);
                await _cartRepository.AddAsync(cart); // Add Cart to Context

                // create new CartItem
                currentItem = new CartItem(cart.Id, request.ProductId, request.Quantity, product);
                cart.Items.Add(currentItem);
            }
            else
            {
                // if alreadly have cart -> check product exist
                currentItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

                if (currentItem != null)
                {
                    // if exist -> plus quantity
                    currentItem.UpdateQuantity(currentItem.Quantity + request.Quantity);
                }
                else
                {
                    // if not -> add new I=item into old cart
                    currentItem = new CartItem(cart.Id, request.ProductId, request.Quantity, product);

                    await _cartRepository.AddItemAsync(currentItem);
                }
            }

            // save
            await _unitOfWork.SaveChangesAsync();

            var itemDtoHelper = new CartItemResponseDto();
            var responseDto = itemDtoHelper.mapToCartItemDto(currentItem);

            return Result<CartItemResponseDto>.Success(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to cart");
            return Result<CartItemResponseDto>.Failure($"Error adding to cart: {ex.Message}");
        }
    }
    public async Task<Result<string>> RemoveAsync(Guid cartItemId)
    {
        try
        {
            var result = await _cartRepository.RemoveItemAsync(cartItemId);
            if (result)
            {
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation($"Item with ID: {cartItemId} removed successfully.");
                return Result<string>.Success("Item removed successfully");
            }
            return Result<string>.Failure("Error occurred while removing Item");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while removing Item with ID: {Id}", cartItemId);
            return Result<string>.Failure($"An error occurred while removing the item: {ex.Message}");
        }
    }

    public async Task<Result<CartItemResponseDto>> UpdateQuantityAsync(Guid userId, UpdateCartItemRequestDto request)
    {
        try
        {
            // get user's cart
            var cart = await _cartRepository.GetCartByUserIdAsync(userId);
            if (cart == null)
            {
                return Result<CartItemResponseDto>.Failure("Cart not found.");
            }

            // found items in cart
            var currentItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
            if (currentItem == null)
            {
                return Result<CartItemResponseDto>.Failure("Product not found in cart.");
            }

            // check stock before update
            if (currentItem.Product != null && currentItem.Product.Stock < request.NewQuantity)
            {
                 return Result<CartItemResponseDto>.Failure($"Not enough stock. Available: {currentItem.Product.Stock}");
            }

            // update quantity
            currentItem.UpdateQuantity(request.NewQuantity);

            // save
            await _unitOfWork.SaveChangesAsync();

            // map entity to dto
            var itemDtoHelper = new CartItemResponseDto();
            var responseDto = itemDtoHelper.mapToCartItemDto(currentItem);

            return Result<CartItemResponseDto>.Success(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart quantity");
            return Result<CartItemResponseDto>.Failure($"Error updating quantity: {ex.Message}");
        }
    }
}