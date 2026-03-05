using DrugStoreWebSiteData.Domain.Entities;

namespace DrugStoreWebSiteData.Application.DTOs;

public class OrderResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; }
    public string ShippingAddress { get; set; }
    public string PhoneNumber { get; set; }
    public string? Note { get; set; }
    public List<OrderItemResponseDto> Items { get; set; } = new();
    public OrderResponseDto() { }

    public OrderResponseDto MapToOrderDto(Order order)
    {
        var itemDtoHelper = new OrderItemResponseDto();
        return new OrderResponseDto
        {
            Id = order.Id,
            UserId = order.UserId,
            OrderDate = order.OrderDate,
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString(),
            ShippingAddress = order.ShippingAddress,
            PhoneNumber = order.PhoneNumber,
            Note = order.Note,
            Items = order.OrderItems.Select(i => itemDtoHelper.mapToOrderItemDto(i)).ToList()
        };
    }
}