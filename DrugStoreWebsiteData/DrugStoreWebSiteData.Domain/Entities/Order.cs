using DrugStoreWebSiteData.Domain.Enums;

namespace DrugStoreWebSiteData.Domain.Entities;

public class Order
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime OrderDate { get; private set; }
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }
    
    public string ShippingAddress { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public string? Note { get; private set; }

    // Navigation Property
    public ICollection<OrderItem> OrderItems { get; private set; } = new List<OrderItem>();

    private Order() { }

    public Order(Guid userId, decimal totalAmount, string address, string phone, string? note)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        OrderDate = DateTime.UtcNow;
        TotalAmount = totalAmount;
        Status = OrderStatus.New;
        ShippingAddress = address;
        PhoneNumber = phone;
        Note = note;
    }
      public void UpdateStatus(OrderStatus status)
        {
            Status = status;
        }
}