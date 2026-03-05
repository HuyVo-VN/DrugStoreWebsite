using System.ComponentModel.DataAnnotations;
using DrugStoreWebSiteData.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace DrugStoreWebSiteData.Application.DTOs.Request;

public class CreateProductRequestDto
{
    public CreateProductRequestDto() { }

    [Required(ErrorMessage = "Product name is required")]
    [StringLength(100, ErrorMessage = "Product name cannot exceed 100 characters")]
    public string Name { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative")]
    public int Stock { get; set; }

    [Required(ErrorMessage = "Category ID is required")]
    public Guid CategoryId { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; }

    public IFormFile? ImageFile { get; set; }

    public int DiscountPercent { get; set; } = 0;

    public DateTime? DiscountEndDate { get; set; }
    public int SaleStock { get; set; }

    public Product mapToProduct()
    {
        return new Product(Name, Description, Price, Stock, CategoryId, DiscountPercent, DiscountEndDate, SaleStock);
    }
}
