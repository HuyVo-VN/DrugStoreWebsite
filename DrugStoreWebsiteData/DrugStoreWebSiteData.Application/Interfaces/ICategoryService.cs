using DrugStoreWebSiteData.Application.Common;
using DrugStoreWebSiteData.Application.DTOs.Request;
using DrugStoreWebSiteData.Application.DTOs.Response;

namespace DrugStoreWebSiteData.Application.Interfaces;

public interface ICategoryService
{
    Task<Result<CategoryResponseDto>> CreateCategoryAsync(CreateCategoryRequestDto request, string currentUser);
    Task<Result<CategoryResponseDto>> GetCategoryByIdAsync(Guid id);
    Task<Result<CategoryResponseDto>> UpdateCategoryAsync(Guid id, UpdateCategoryRequestDto request, string currentUser);
    Task<Result<string>> DeleteAsync(Guid categoryId);
    Task<Result<string>> UpdateStatusAsync(UpdateStatusCategoryRequestDto request, string currentUser);
    Task<Result<PagedResult<CategoryResponseDto>>> GetAllCategoriesPagedAsync(int pageNumber, int pageSize);
    Task<Result<List<CategoryResponseDto>>> GetAllCategoriesAsync();
}