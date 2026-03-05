
using System.ComponentModel.DataAnnotations;

namespace DrugStoreWebSiteData.Application.DTOs
{
    public class FilterProductRequestDto
    {
        public FilterProductRequestDto() { }

        [Required(ErrorMessage = "Category id is required")]
        public Guid CategoryId { get; set; }
        [Required(ErrorMessage = "Page size is required")]
        public int PageSize { get; set; }
        [Required(ErrorMessage = "Page number is required")]
        public int PageNumber { get; set; }
    }
}