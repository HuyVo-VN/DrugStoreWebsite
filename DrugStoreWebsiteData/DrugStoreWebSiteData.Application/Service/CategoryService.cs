using DrugStoreWebSiteData.Application.Common;
using DrugStoreWebSiteData.Application.DTOs.Request;
using DrugStoreWebSiteData.Application.DTOs.Response;
using DrugStoreWebSiteData.Application.Interfaces;
using DrugStoreWebSiteData.Domain.Entities;
using DrugStoreWebSiteData.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace DrugStoreWebSiteData.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CategoryService> _logger;
    private readonly IDistributedCache _cache;

    public CategoryService(
        ICategoryRepository categoryRepository, 
        IUnitOfWork unitOfWork, 
        ILogger<CategoryService> logger,
        IDistributedCache cache
        )
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cache = cache;
    }

    public async Task<Result<CategoryResponseDto>> CreateCategoryAsync(CreateCategoryRequestDto request, string currentUser)
    {
        try
        {
            var category = new Category(request.Name, request.Description);
            category.UpdateDetails(request.Name, request.Description, currentUser);

            await _categoryRepository.AddAsync(category);
            await _unitOfWork.SaveChangesAsync();

            var responseDto = new CategoryResponseDto().mapToCategoryDto(category);
            await _cache.RemoveAsync("categories_all");
            _logger.LogInformation("Category created successfully: {CategoryId}", category.Id);

            return Result<CategoryResponseDto>.Success(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating category");
            return Result<CategoryResponseDto>.Failure($"An error occurred: {ex.Message}");
        }
    }

    public async Task<Result<CategoryResponseDto>> UpdateCategoryAsync(Guid id, UpdateCategoryRequestDto request, string currentUser)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                _logger.LogWarning("Category not found: {CategoryId}", id);
                return Result<CategoryResponseDto>.Failure("Category not found.");
            }

            category.UpdateDetails(request.Name, request.Description, currentUser);
            _categoryRepository.Update(category);
            await _unitOfWork.SaveChangesAsync();

            var responseDto = new CategoryResponseDto().mapToCategoryDto(category);
            await _cache.RemoveAsync("categories_all");
            _logger.LogInformation("Category updated successfully: {CategoryId}", category.Id);

            return Result<CategoryResponseDto>.Success(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating category");
            return Result<CategoryResponseDto>.Failure($"An error occurred: {ex.Message}");
        }
    }

    public async Task<Result<CategoryResponseDto>> GetCategoryByIdAsync(Guid id)
    {
        try
        {
            var categoryResult = await _categoryRepository.GetByIdAsync(id);
            if (categoryResult == null)
            {
                _logger.LogWarning("Category with ID: {CategoryId} not found.", id);
                return Result<CategoryResponseDto>.Failure("Category not found.");
            }

            var responseDto = new CategoryResponseDto().mapToCategoryDto(categoryResult);
            _logger.LogInformation("Category retrieved successfully: {CategoryId}", id);

            return Result<CategoryResponseDto>.Success(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving category");
            return Result<CategoryResponseDto>.Failure($"An error occurred: {ex.Message}");
        }
    }

    public async Task<Result<string>> DeleteAsync(Guid categoryId)
    {
        try
        {
            var result = await _categoryRepository.DeleteAsync(categoryId);
            if (result)
            {
                await _unitOfWork.SaveChangesAsync();
                await _cache.RemoveAsync("categories_all");
                _logger.LogInformation("Category with ID: {CategoryId} deleted successfully.", categoryId);
                return Result<string>.Success("Category deleted successfully.");
            }
            return Result<string>.Failure("Category not found or could not be deleted.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting category with ID: {Id}", categoryId);
            return Result<string>.Failure($"An error occurred while deleting the category: {ex.Message}");
        }
    }

    public async Task<Result<string>> UpdateStatusAsync(UpdateStatusCategoryRequestDto request, string currentUser)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
            if (category != null)
            {
                category.UpdateStatus(request.NewStatus, currentUser);
                _categoryRepository.Update(category);

                await _unitOfWork.SaveChangesAsync();
                await _cache.RemoveAsync("categories_all");

                _logger.LogInformation("Category status updated successfully for ID: {CategoryId}", category.Id);
                return Result<string>.Success("Category status updated successfully.");
            }

            return Result<string>.Failure("Error occurred while getting category to update.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating category status");
            return Result<string>.Failure($"An error occurred while updating the category status: {ex.Message}");
        }
    }

    public async Task<Result<PagedResult<CategoryResponseDto>>> GetAllCategoriesPagedAsync(int pageNumber, int pageSize)
    {
        try
        {
            var (categories, totalCount) = await _categoryRepository.GetPagedAsync(pageNumber, pageSize);

            if (categories == null)
            {
                _logger.LogError("Failed to retrieve categories");
                return Result<PagedResult<CategoryResponseDto>>.Failure("Category list not found");
            }

            var responseDtos = categories.Select(c => new CategoryResponseDto().mapToCategoryDto(c)).ToList();
            var pagedResult = new PagedResult<CategoryResponseDto>(responseDtos, totalCount, pageNumber, pageSize);

            _logger.LogInformation("Paged categories retrieved successfully.");
            return Result<PagedResult<CategoryResponseDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving paged categories");
            return Result<PagedResult<CategoryResponseDto>>.Failure($"An error occurred: {ex.Message}");
        }
    }

    public async Task<Result<List<CategoryResponseDto>>> GetAllCategoriesAsync()
    {
        try
        {
            string cacheKey = "categories_all";

            // 1. NGÓ VÀO KHO REDIS TRƯỚC
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.LogInformation("Lấy Categories từ REDIS CACHE thành công!");
                var cachedCategories = JsonSerializer.Deserialize<List<CategoryResponseDto>>(cachedData);
                return Result<List<CategoryResponseDto>>.Success(cachedCategories);
            }

            // 2. NẾU REDIS KHÔNG CÓ -> CHẠY XUỐNG SQL SERVER (Code cũ)
            var categories = await _categoryRepository.GetAllAsync();
            var responseDtos = categories.Select(c => new CategoryResponseDto().mapToCategoryDto(c)).ToList();

            // 3. LƯU LẠI VÀO REDIS ĐỂ LẦN SAU DÙNG (Cất kho trong 1 ngày)
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
            };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(responseDtos), cacheOptions);

            _logger.LogInformation("All categories retrieved successfully from Database.");
            return Result<List<CategoryResponseDto>>.Success(responseDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving all categories");
            return Result<List<CategoryResponseDto>>.Failure($"An error occurred: {ex.Message}");
        }
    }
}