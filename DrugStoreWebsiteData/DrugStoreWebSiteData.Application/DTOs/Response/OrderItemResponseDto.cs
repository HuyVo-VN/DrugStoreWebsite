using DrugStoreWebSiteData.Domain.Entities;

namespace DrugStoreWebSiteData.Application.DTOs;

public class OrderItemResponseDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public string ProductImage { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }

    public OrderItemResponseDto mapToOrderItemDto(OrderItem item)
    {
        return new OrderItemResponseDto
        {
            Id=item.Id,
            OrderId=item.OrderId,
            ProductId = item.ProductId,
            ProductName = item.Product?.Name ?? "Unknown Product",
            ProductImage = item.Product?.ImageUrl ?? "",
            Price = item.Product?.Price ?? 0,
            Quantity = item.Quantity,
            TotalPrice = (item.Product?.Price ?? 0) * item.Quantity,
        };
    }
}