using System.ComponentModel.DataAnnotations;

namespace DrugStoreWebSiteData.Application.DTOs.Request;

public class CreateOrderItemRequestDto
{
    [Required]
    public Guid ProductId { get; set; }
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero")]
    public int Quantity { get; set; }

}