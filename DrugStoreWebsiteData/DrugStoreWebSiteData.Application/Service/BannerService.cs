using DrugStoreWebSiteData.Application.DTOs.Request;
using DrugStoreWebSiteData.Application.DTOs.Response;

using DrugStoreWebSiteData.Domain.Interfaces;
using DrugStoreWebSiteData.Application.Interfaces;
using Microsoft.Extensions.Logging;
using DrugStoreWebSiteData.Application.Common;
using DrugStoreWebSiteData.Domain.Entities;
using DrugStoreWebSiteData.Application.DTOs;
using System.Reflection.Metadata;
namespace DrugStoreWebSiteData.Application.Services;

public class BannerService : IBannerService
{
    private readonly IBannerRepository _bannerRepository;
    private readonly IImageService _imageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BannerService> _logger;

    public BannerService(
        IBannerRepository bannerRepository,
        IUnitOfWork unitOfWork,
        ILogger<BannerService> logger,
        IImageService imageService)
    {
        _bannerRepository = bannerRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _imageService = imageService;
    }

    public async Task<IEnumerable<BannerResponseDto>> GetActiveBannersAsync()
    {
        try
        {
            var banners = await _bannerRepository.GetActiveBannersAsync();
            var bannerDtos = new List<BannerResponseDto>();

            foreach (var banner in banners)
            {
                bannerDtos.Add(new BannerResponseDto
                {
                    Id = banner.Id,
                    Title = banner.Title,
                    ImageUrl = banner.ImageUrl,
                    TargetUrl = banner.TargetUrl,
                    DisplayOrder = banner.DisplayOrder
                });
            }

            _logger.LogInformation("Active banners retrieved successfully.");
            return bannerDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving active banners");
            return new List<BannerResponseDto>();
        }
    }

    public async Task<Result<IEnumerable<BannerResponseDto>>> GetAllBannersAsync()
    {
        var banners = await _bannerRepository.GetAllBannersAsync();
        var bannerDtos = banners.Select(b => new BannerResponseDto
        {
            Id = b.Id,
            Title = b.Title,
            ImageUrl = b.ImageUrl,
            TargetUrl = b.TargetUrl,
            DisplayOrder = b.DisplayOrder,
            IsActive = b.IsActive
        }).ToList();

        return Result<IEnumerable<BannerResponseDto>>.Success(bannerDtos);
    }

    public async Task<Result<BannerResponseDto>> CreateBannerAsync(CreateBannerRequestDto request)
    {
        try
        {
            string imageUrl = string.Empty;
            if (request.ImageFile != null)
            {
                imageUrl = await _imageService.SaveImageAsync(request.ImageFile, "banners");
            }

            var banner = new Banner
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                TargetUrl = request.TargetUrl,
                DisplayOrder = request.DisplayOrder,
                ImageUrl = imageUrl,
                IsActive = true
            };

            await _bannerRepository.AddAsync(banner);
            await _unitOfWork.SaveChangesAsync();

            var dto = new BannerResponseDto { Id = banner.Id, Title = banner.Title, ImageUrl = banner.ImageUrl, TargetUrl = banner.TargetUrl, DisplayOrder = banner.DisplayOrder, IsActive = banner.IsActive };
            return Result<BannerResponseDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating banner");
            return Result<BannerResponseDto>.Failure("Failed to create banner");
        }
    }

    public async Task<Result<BannerResponseDto>> UpdateBannerAsync(Guid id, UpdateBannerRequestDto request)
    {
        try
        {
            var banner = await _bannerRepository.GetByIdAsync(id);
            if (banner == null) return Result<BannerResponseDto>.Failure("Banner not found");

            if (request.ImageFile != null)
            {
                if (!string.IsNullOrEmpty(banner.ImageUrl)) _imageService.DeleteImage(banner.ImageUrl);
                banner.ImageUrl = await _imageService.SaveImageAsync(request.ImageFile, "banners");
            }
            else if (request.DeleteCurrentImage)
            {
                if (!string.IsNullOrEmpty(banner.ImageUrl)) _imageService.DeleteImage(banner.ImageUrl);
                banner.ImageUrl = string.Empty;
            }

            banner.Title = request.Title;
            banner.TargetUrl = request.TargetUrl;
            banner.DisplayOrder = request.DisplayOrder;
            banner.IsActive = request.IsActive;

            _bannerRepository.Update(banner);
            await _unitOfWork.SaveChangesAsync();

            var dto = new BannerResponseDto { Id = banner.Id, Title = banner.Title, ImageUrl = banner.ImageUrl, TargetUrl = banner.TargetUrl, DisplayOrder = banner.DisplayOrder, IsActive = banner.IsActive };
            return Result<BannerResponseDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating banner");
            return Result<BannerResponseDto>.Failure("Failed to update banner");
        }
    }

    public async Task<Result<string>> DeleteBannerAsync(Guid id)
    {
        try
        {
            var banner = await _bannerRepository.GetByIdAsync(id);
            if (banner != null && !string.IsNullOrEmpty(banner.ImageUrl))
            {
                _imageService.DeleteImage(banner.ImageUrl);
            }

            var result = await _bannerRepository.DeleteAsync(id);
            if (result)
            {
                await _unitOfWork.SaveChangesAsync();
                return Result<string>.Success("Banner deleted successfully");
            }
            return Result<string>.Failure("Failed to delete banner");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting banner");
            return Result<string>.Failure("Error deleting banner");
        }
    }
}