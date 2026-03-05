namespace DrugStoreWebSiteData.Domain.Entities;

public class Cart
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }

    public ICollection<CartItem> Items { get; private set; } = new List<CartItem>();

    private Cart() { }

    public Cart(Guid userId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
    }
}