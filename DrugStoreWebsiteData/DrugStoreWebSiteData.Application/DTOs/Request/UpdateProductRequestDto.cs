using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DrugStoreWebSiteData.Application.DTOs.Request;
public class UpdateProductRequestDto
{
    public UpdateProductRequestDto()
    {
    }
    [Required(ErrorMessage = "Product name is required")]
    [StringLength(100, ErrorMessage = "Product name cannot exceed 100 characters.")]
    public string Name { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string Description { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative.")]
    public int Stock{ get; set; }
    public IFormFile? ImageFile { get; set; }
    public bool DeleteCurrentImage { get; set; }

    public int DiscountPercent { get; set; } = 0;

    public DateTime? DiscountEndDate { get; set; }
    public int SaleStock { get; set; } = 0;

    [Required(ErrorMessage = "Category ID is required")]
    public Guid CategoryId { get; set; }

    public string Specifications { get; set; }
}