using System.ComponentModel.DataAnnotations;

namespace DrugStoreWebSiteData.Application.DTOs.Request;

public class CreateOrderRequestDto
{
    [Required]
    public decimal TotalAmount { get; set; }
    [Required]
    [StringLength(100)]
    public string ShippingAddress { get; set; }
    [Required]
    [StringLength(15)]
    public string PhoneNumber { get; set; }
    
    [Required]
    public List<CreateOrderItemRequestDto> Items { get; set; }

}