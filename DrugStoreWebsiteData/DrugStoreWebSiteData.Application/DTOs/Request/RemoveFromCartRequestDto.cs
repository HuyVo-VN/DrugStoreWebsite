using System.ComponentModel.DataAnnotations;

namespace DrugStoreWebSiteData.Application.DTOs.Request;

public class RemoveFromCartRequestDto
{
    public RemoveFromCartRequestDto() { }

    [Required(ErrorMessage = "Item Id is required")] 
    public Guid ItemId { get; set; }
}