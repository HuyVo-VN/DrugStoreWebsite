using Microsoft.AspNetCore.Http;

namespace DrugStoreWebSiteData.Application.DTOs.Response;
public class BannerResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}