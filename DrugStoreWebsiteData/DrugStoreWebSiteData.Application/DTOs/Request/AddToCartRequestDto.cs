using System.ComponentModel.DataAnnotations;

namespace DrugStoreWebSiteData.Application.DTOs.Request;

public class AddToCartRequestDto
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; } = 1;
}