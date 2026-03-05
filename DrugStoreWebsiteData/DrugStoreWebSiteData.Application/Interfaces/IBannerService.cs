using DrugStoreWebSiteData.Application.DTOs.Request;
using DrugStoreWebSiteData.Application.DTOs.Response;

using DrugStoreWebSiteData.Application.Common;
using DrugStoreWebSiteData.Application.DTOs;

namespace DrugStoreWebSiteData.Application.Interfaces;

public interface IBannerService
{
    Task<IEnumerable<BannerResponseDto>> GetActiveBannersAsync();
    Task<Result<IEnumerable<BannerResponseDto>>> GetAllBannersAsync();
    Task<Result<BannerResponseDto>> CreateBannerAsync(CreateBannerRequestDto request);
    Task<Result<BannerResponseDto>> UpdateBannerAsync(Guid id, UpdateBannerRequestDto request);
    Task<Result<string>> DeleteBannerAsync(Guid id);
}