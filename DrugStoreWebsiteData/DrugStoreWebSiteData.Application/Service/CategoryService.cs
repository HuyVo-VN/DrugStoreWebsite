using DrugStoreWebSiteData.Application.Common;
using DrugStoreWebSiteData.Application.DTOs.Request;
using DrugStoreWebSiteData.Application.DTOs.Response;
using DrugStoreWebSiteData.Application.Interfaces;
using DrugStoreWebSiteData.Domain.Interfaces;
using Microsoft.Extensions.Logging;


namespace DrugStoreWebSiteData.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ILogger<CategoryService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(
        ILogger<CategoryService> logger,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CategoryResponseDto>> CreateCategoryAsync(CreateCategoryRequestDto request)
    {
        try
        {
            var existingCategory = await _categoryRepository.GetByNameAsync(request.Name);
            if (existingCategory != null)
            {
                _logger.LogWarning("Category creation failed. Category with name: {CategoryName} already exists.", request.Name);
                return Result<CategoryResponseDto>.Failure("Category with the same name already exists.");
            }

            var newCategory = request.mapToCategory();

            await _categoryRepository.AddAsync(newCategory);

            await _unitOfWork.SaveChangesAsync();

            var responseDto = new CategoryResponseDto();
            _logger.LogInformation("Category created successfully: {CategoryName}", request.Name);
            return Result<CategoryResponseDto>.Success(responseDto.mapToCategoryDto(newCategory));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating category: {CategoryName}", request.Name);
            return Result<CategoryResponseDto>.Failure($"Error occurred while creating category: {ex.Message}");
        }


    }

    public async Task<Result<CategoryResponseDto>> GetCategoryByIdAsync(Guid id)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                _logger.LogWarning("Category retrieval failed. Category with ID: {CategoryId} does not exist.", id);
                return Result<CategoryResponseDto>.Failure("Category not found.");
            }

            var responseDto = new CategoryResponseDto();
            _logger.LogInformation("Category retrieved successfully: {CategoryId}", id);
            return Result<CategoryResponseDto>.Success(responseDto.mapToCategoryDto(category));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving category with ID: {CategoryId}", id);
            return Result<CategoryResponseDto>.Failure("An error occurred while retrieving the category.");
        }
    }

    public async Task<Result<List<CategoryResponseDto>>> GetAllCategoriesAsync()
    {
        try
        {
            var categoryResult = await _categoryRepository.GetAllAsync();
            if (categoryResult == null)
            {
                _logger.LogWarning("Failed to retrieve categories");
                return Result<List<CategoryResponseDto>>.Failure("Failed to get all categories");
            }

            var responseDtos = new List<CategoryResponseDto>();
            var dtoHelper = new CategoryResponseDto();

            foreach (var category in categoryResult)
            {
                responseDtos.Add(dtoHelper.mapToCategoryDto(category));
            }

            _logger.LogInformation("All categories retrieved successfully.");
            return Result<List<CategoryResponseDto>>.Success(responseDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving categories");
            return Result<List<CategoryResponseDto>>.Failure($"An error occurred: {ex.Message}");
        }
    }


}