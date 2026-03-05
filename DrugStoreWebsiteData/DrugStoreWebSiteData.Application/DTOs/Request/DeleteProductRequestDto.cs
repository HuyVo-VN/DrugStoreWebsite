
using System.ComponentModel.DataAnnotations;

namespace DrugStoreWebSiteData.Application.DTOs
{
    public class DeleteProductRequestDto
    {
        public DeleteProductRequestDto() { }

        [Required(ErrorMessage = "Product Id is required")]
        public Guid ProductId { get; set; }
    }
}