using DrugStoreWebSiteData.Application.DTOs.Request;
using DrugStoreWebSiteData.Application.DTOs.Response;
using DrugStoreWebSiteData.Application.Common;

namespace DrugStoreWebSiteData.Application.Interfaces;

public interface ICategoryService
{
    Task<Result<CategoryResponseDto>> CreateCategoryAsync(CreateCategoryRequestDto request);
    Task<Result<CategoryResponseDto>> GetCategoryByIdAsync(Guid id);
    Task<Result<List<CategoryResponseDto>>> GetAllCategoriesAsync();
}