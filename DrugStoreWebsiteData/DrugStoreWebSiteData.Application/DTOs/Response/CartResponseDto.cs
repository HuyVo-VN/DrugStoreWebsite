using DrugStoreWebSiteData.Domain.Entities;

namespace DrugStoreWebSiteData.Application.DTOs;

public class CartResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public List<CartItemResponseDto> Items { get; set; } = new();
    public decimal GrandTotal { get; set; }

    public CartResponseDto mapToCartDto(Cart cart)
    {
        var itemDtoHelper = new CartItemResponseDto();

        return new CartResponseDto
        {
            Id = cart.Id,
            UserId = cart.UserId,
            Items = cart.Items.Select(i => itemDtoHelper.mapToCartItemDto(i)).ToList()
        };
    }
}