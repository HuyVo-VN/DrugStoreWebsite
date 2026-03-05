namespace DrugStoreWebSiteData.Domain.Entities;

public class OrderItem
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string ProductImage { get; private set; } = string.Empty;
    
    public decimal Price { get; private set; }
    public int Quantity { get; private set; }

    public Order Order { get; private set; } = null!;
    public Product Product { get; private set; } = null!;

    private OrderItem() { }

    public OrderItem(Guid orderId, Guid productId, string productName, string productImage, decimal price, int quantity, Product product)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        ProductId = productId;
        ProductName = productName;
        ProductImage = productImage;
        Price = price;
        Quantity = quantity;
        Product = product;
    }
}