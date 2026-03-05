using DrugStoreWebSiteData.Domain.Entities;

namespace DrugStoreWebSiteData.Application.DTOs.Response;

public class ProductResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string ImageUrl { get; set; }
    public Guid CategoryId { get; set; }
    public bool IsActive { get; set; }
    public int DiscountPercent { get; set; }
    public int SoldQuantity { get; set; }
    public DateTime? DiscountEndDate { get; set; }
    public int SaleStock { get; set; } = 0;
    public int SaleSold { get; set; } = 0;

    public ProductResponseDto mapToProductDto(Product product)
    {
        return new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock,
            ImageUrl = product.ImageUrl,
            CategoryId = product.CategoryId,
            IsActive = product.IsActive,
            DiscountPercent = product.DiscountPercent,
            SoldQuantity = product.SoldQuantity,
            DiscountEndDate = product.DiscountEndDate,
            SaleStock = product.SaleStock,
            SaleSold = product.SaleSold,
        };
    }
}