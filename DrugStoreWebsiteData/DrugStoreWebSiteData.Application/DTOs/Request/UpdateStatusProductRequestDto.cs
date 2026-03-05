
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using DrugStoreWebSiteData.Domain.Entities;

namespace DrugStoreWebSiteData.Application.DTOs
{
    public class UpdateStatusProductRequestDto
    {
        public UpdateStatusProductRequestDto() { }

        [Required(ErrorMessage = "Product Id is required")]
        public Guid ProductId { get; set; }
        [Required(ErrorMessage = "Product new status is required")]
        public bool NewStatus { get; set; }
        [Required(ErrorMessage = "Updated By is required")]
        [StringLength(50)]
        public string UpdatedBy { get; set; }
        [Required(ErrorMessage = "Updated At is required")]
        public DateTime UpdatedAt { get; set; }   

    }
}