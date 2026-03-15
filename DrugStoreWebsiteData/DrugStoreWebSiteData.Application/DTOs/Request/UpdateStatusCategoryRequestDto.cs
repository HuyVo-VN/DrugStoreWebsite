using System.ComponentModel.DataAnnotations;

namespace DrugStoreWebSiteData.Application.DTOs.Request
{
    public class UpdateStatusCategoryRequestDto
    {
        [Required]
        public Guid CategoryId { get; set; }

        [Required]
        public bool NewStatus { get; set; }
    }
}
