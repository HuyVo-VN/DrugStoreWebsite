using DrugStoreWebSiteData.Domain.Entities;

public class CartItemResponseDto
{
    public Guid ProductId { get; set; }
    public Guid ItemId { get; set; }
    public string ProductName { get; set; }
    public string ProductImage { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int Stock { get; set; }
    public decimal TotalPrice { get; set; }
    public bool IsActive { get; set; }
    public int DiscountPercent { get; set; }
    public DateTime? DiscountEndDate { get; set; }
    public int SaleStock { get; set; } = 0;
    public int SaleSold { get; set; } = 0;

    public CartItemResponseDto mapToCartItemDto(CartItem item)
    {
        return new CartItemResponseDto
        {
            ProductId = item.ProductId,
            ItemId = item.Id,
            ProductName = item.Product?.Name ?? "Unknown Product",
            ProductImage = item.Product?.ImageUrl ?? "",
            Price = item.Product?.Price ?? 0,
            Quantity = item.Quantity,
            TotalPrice = (item.Product?.Price ?? 0) * item.Quantity,
            Stock = item.Product?.Stock ?? 0,
            IsActive = item.Product?.IsActive ?? false,
            DiscountPercent = item.Product?.DiscountPercent ?? 0,
            DiscountEndDate = item.Product?.DiscountEndDate,
            SaleStock = item.Product?.SaleStock ?? 0,
            SaleSold = item.Product?.SaleSold ?? 0,
        };
    }
}