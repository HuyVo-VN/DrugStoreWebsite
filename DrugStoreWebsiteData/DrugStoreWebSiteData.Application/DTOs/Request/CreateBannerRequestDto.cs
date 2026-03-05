using Microsoft.AspNetCore.Http;

namespace DrugStoreWebSiteData.Application.DTOs.Request
{
    public class CreateBannerRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string TargetUrl { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 0;
        public IFormFile? ImageFile { get; set; }
    }
}