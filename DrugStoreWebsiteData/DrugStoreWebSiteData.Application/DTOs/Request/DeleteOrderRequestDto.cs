
using System.ComponentModel.DataAnnotations;

namespace DrugStoreWebSiteData.Application.DTOs
{
    public class DeleteOrderRequestDto
    {
        public DeleteOrderRequestDto() { }

        [Required(ErrorMessage = "Order Id is required")]
        public Guid OrderId { get; set; }
    }
}