namespace DrugStoreWebSiteData.Domain.Entities;

public class CartItem
{
    public Guid Id { get; private set; }
    public Guid CartId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }

    // Navigation Properties
    public Cart Cart { get; private set; } = null!;
    public Product Product { get; private set; } = null!;

    private CartItem() { }

    public CartItem(Guid cartId, Guid productId, int quantity, Product product)
    {
        Id = Guid.NewGuid();
        CartId = cartId;
        ProductId = productId;
        Quantity = quantity;
        Product = product;
    }
    public void UpdateQuantity(int quantity) { Quantity = quantity; }
}