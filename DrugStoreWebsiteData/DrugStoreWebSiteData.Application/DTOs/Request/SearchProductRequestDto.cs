
using System.ComponentModel.DataAnnotations;

namespace DrugStoreWebSiteData.Application.DTOs
{
    public class SearchProductRequestDto
    {
        public SearchProductRequestDto() { }

        [Required(ErrorMessage = "Product name is required")]
        public string ProductName { get; set; }
        [Required(ErrorMessage = "Page size is required")]
        public int PageSize { get; set; }
        [Required(ErrorMessage = "Page number is required")]
        public int PageNumber { get; set; }
    }
}