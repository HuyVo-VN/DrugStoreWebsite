using Microsoft.AspNetCore.Http;

namespace DrugStoreWebSiteData.Application.DTOs.Request
{
    public class UpdateBannerRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string TargetUrl { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; }
        public IFormFile? ImageFile { get; set; }
        public bool DeleteCurrentImage { get; set; } = false;
    }
}