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

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IImageService _imageService;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        ILogger<ProductService> logger,
        IImageService imageService)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _imageService = imageService;
    }

    public async Task<Result<ProductResponseDto>> CreateProductAsync(CreateProductRequestDto request)
    {
        try
        {
            // Check if category exists
            var categoryResult = await _categoryRepository.GetByIdAsync(request.CategoryId);
            if (categoryResult == null)
            {
                _logger.LogError("Product creation failed. Category with ID: {CategoryId} does not exist.", request.CategoryId);
                return Result<ProductResponseDto>.Failure("Category does not exist.");
            }

            // Map DTO to Entity
            var product = request.mapToProduct();

            // Image
            if (request.ImageFile != null)
            {
                // save and take url
                var newImageUrl = await _imageService.SaveImageAsync(request.ImageFile);

                // update URL into Entity
                product.UpdateDetails(
                    product.Name,
                    product.Description,
                    product.Price,
                    product.Stock,
                    newImageUrl, // use URL
                    product.CategoryId,
                    product.DiscountPercent,
                    product.DiscountEndDate,
                    product.SaleStock
                );
            }

            // Add product to repository
            await _productRepository.AddAsync(product);

            // Save changes
            await _unitOfWork.SaveChangesAsync();

            // Return the created product as ProductDto
            var responseDto = new ProductResponseDto();
            _logger.LogInformation("Product created successfully: {ProductId}", product.Id);
            return Result<ProductResponseDto>.Success(responseDto.mapToProductDto(product));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating product");
            return Result<ProductResponseDto>.Failure($"An error occurred: {ex.Message}");
        }
    }

    public async Task<Result<ProductResponseDto>> GetProductByIdAsync(Guid id)
    {
        try
        {
            var productResult = await _productRepository.GetByIdAsync(id);
            if (productResult == null)
            {
                _logger.LogWarning("Product with ID: {ProductId} not found.", id);
                return Result<ProductResponseDto>.Failure("Product not found.");
            }

            var responseDto = new ProductResponseDto();
            _logger.LogInformation("Product retrieved successfully: {ProductId}", id);
            return Result<ProductResponseDto>.Success(responseDto.mapToProductDto(productResult));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving product");
            return Result<ProductResponseDto>.Failure($"An error occurred: {ex.Message}");
        }
    }

    public async Task<Result<string>> DeleteAsync(Guid productId)
    {
        try
        {
            var result = await _productRepository.DeleteAsync(productId);
            if (result)
            {
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation($"Product with ID: {productId} deleted successfully.");
                return Result<string>.Success("Product deleted successfully");
            }
            return Result<string>.Failure("Error occurred while deleting product");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting product with ID: {Id}", productId);
            return Result<string>.Failure($"An error occurred while deleting the product: {ex.Message}");
        }
    }
    public async Task<Result<string>> UpdateStatusAsync(UpdateStatusProductRequestDto requestDto)
    {
        try
        {
            var productDetail = await _productRepository.GetByIdAsync(requestDto.ProductId);
            if (productDetail != null)
            {
                try
                {
                    productDetail.UpdateStatus(requestDto.NewStatus, requestDto.UpdatedBy, requestDto.UpdatedAt);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"An error occurred while updating status product detail: {ex.Message}");
                    return Result<string>.Failure($"An error occurred while updating status product detail: {ex.Message}");
                }

                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation($"Product with ID: {productDetail.Id} updated successfully.");
                return Result<string>.Success("Product updated successfully");
            }

            return Result<string>.Failure("Error occurred while geting product to update ");
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred while updating product: {ex.Message}");
            return Result<string>.Failure($"An error occurred while updating the product: {ex.Message}");
        }
    }

    public async Task<Result<ProductResponseDto>> UpdateProductAsync(Guid id, UpdateProductRequestDto request)
    {
        try
        {
            var productResult = await _productRepository.GetByIdAsync(id);
            if (productResult == null)
            {
                _logger.LogError("Failed to update product. Product with ID: {ProductId} does not exist.", id);
                return Result<ProductResponseDto>.Failure("Product not found");
            }

            var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
            if (category == null)
            {
                _logger.LogWarning("Update failed. Category not found: {CategoryId}", request.CategoryId);
                return Result<ProductResponseDto>.Failure("Category not found");
            }

            //image
            string updatedImageUrl = productResult.ImageUrl; // keep old picture

            if (request.ImageFile != null)
            {
                // delete old picture
                if (!string.IsNullOrEmpty(productResult.ImageUrl))
                {
                    _imageService.DeleteImage(productResult.ImageUrl);
                }
                // save new picture
                updatedImageUrl = await _imageService.SaveImageAsync(request.ImageFile);
            }
            else if (request.DeleteCurrentImage)
            {
                if (!string.IsNullOrEmpty(productResult.ImageUrl))
                {
                    _imageService.DeleteImage(productResult.ImageUrl);
                }
                updatedImageUrl = "";
            }

            productResult.UpdateDetails(
                request.Name,
                request.Description,
                request.Price,
                request.Stock,
                updatedImageUrl,
                request.CategoryId,
                request.DiscountPercent,
                request.DiscountEndDate,
                request.SaleStock);

            _productRepository.Update(productResult);

            await _unitOfWork.SaveChangesAsync();

            var productResponse = new ProductResponseDto();
            _logger.LogInformation("Product with ID: {ProductId} updated successfully.", id);
            return Result<ProductResponseDto>.Success(productResponse.mapToProductDto(productResult));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating product with ID: {ProductId}", id);
            return Result<ProductResponseDto>.Failure($"An error occurred while updating the product: {ex.Message}");
        }
    }

    public async Task<Result<PagedResult<ProductResponseDto>>> GetAllProductsAsync(int pageNumber, int pageSize)
    {
        try
        {

            var (products, totalCount) = await _productRepository.GetPagedAsync(pageNumber, pageSize);

            if (products == null)
            {
                _logger.LogError("Failed to retrieve products");
                return Result<PagedResult<ProductResponseDto>>.Failure("Product list not found");
            }
            var responseDtos = new List<ProductResponseDto>();
            var dtoHelper = new ProductResponseDto();

            foreach (var product in products)
            {
                responseDtos.Add(dtoHelper.mapToProductDto(product));
            }

            var pagedResult = new PagedResult<ProductResponseDto>(responseDtos, totalCount, pageNumber, pageSize);

            _logger.LogInformation("All categories retrieved successfully.");
            return Result<PagedResult<ProductResponseDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving all products");
            return Result<PagedResult<ProductResponseDto>>.Failure($"An error occurred while retrieving products: {ex.Message}");
        }
    }
    public async Task<Result<PagedResult<ProductResponseDto>>> SearchProductsAsync(SearchProductRequestDto requestDto)
    {
        try
        {
            var (products, totalCount) = await _productRepository.SearchByNameAsync(requestDto.ProductName, requestDto.PageNumber, requestDto.PageSize);
            if (products == null)
            {
                _logger.LogError("Failed to retrieve products");
                return Result<PagedResult<ProductResponseDto>>.Failure("Product list not found");
            }
            var responseDtos = new List<ProductResponseDto>();
            var dtoHelper = new ProductResponseDto();

            foreach (var product in products)
            {
                responseDtos.Add(dtoHelper.mapToProductDto(product));
            }

            var pagedResult = new PagedResult<ProductResponseDto>(responseDtos, totalCount, requestDto.PageNumber, requestDto.PageSize);
            _logger.LogInformation("All products search by name retrieved successfully.");

            return Result<PagedResult<ProductResponseDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while searching products");
            return Result<PagedResult<ProductResponseDto>>.Failure("Failed to search products");
        }
    }
    public async Task<Result<PagedResult<ProductResponseDto>>> FilterProductsAsync(FilterProductRequestDto requestDto)
    {
        try
        {
            var (products, totalCount) = await _productRepository.FilterByCategoryAsync(requestDto.CategoryId, requestDto.PageNumber, requestDto.PageSize);
            var responseDtos = new List<ProductResponseDto>();
            var dtoHelper = new ProductResponseDto();

            foreach (var product in products)
            {
                responseDtos.Add(dtoHelper.mapToProductDto(product));
            }

            var pagedResult = new PagedResult<ProductResponseDto>(responseDtos, totalCount, requestDto.PageNumber, requestDto.PageSize);
            _logger.LogInformation("All products filter by category retrieved successfully.");

            return Result<PagedResult<ProductResponseDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while filtering products");
            return Result<PagedResult<ProductResponseDto>>.Failure("Failed to filtering products");
        }
    }

    public async Task<Result<PagedResult<ProductResponseDto>>> GetSaleProductsAsync(int limit = 10)
    {
        try
        {
            var products = await _productRepository.GetSaleProductsAsync(limit);

            var responseDtos = new List<ProductResponseDto>();
            var dtoHelper = new ProductResponseDto();

            foreach (var product in products)
            {
                responseDtos.Add(dtoHelper.mapToProductDto(product));
            }

            var pagedResult = new PagedResult<ProductResponseDto>(responseDtos, responseDtos.Count, 1, limit);

            return Result<PagedResult<ProductResponseDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving sale products");
            return Result<PagedResult<ProductResponseDto>>.Failure($"An error occurred: {ex.Message}");
        }
    }

    public async Task<Result<PagedResult<ProductResponseDto>>> GetBestSellerProductsAsync(int limit = 10)
    {
        try
        {
            var products = await _productRepository.GetBestSellerProductsAsync(limit);

            var responseDtos = new List<ProductResponseDto>();
            var dtoHelper = new ProductResponseDto();

            foreach (var product in products)
            {
                responseDtos.Add(dtoHelper.mapToProductDto(product));
            }

            var pagedResult = new PagedResult<ProductResponseDto>(responseDtos, responseDtos.Count, 1, limit);

            return Result<PagedResult<ProductResponseDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving best seller products");
            return Result<PagedResult<ProductResponseDto>>.Failure($"An error occurred: {ex.Message}");
        }
    }

    public async Task<Result<PagedResult<ProductResponseDto>>> GetProductsByCollectionNameAsync(string collectionName, int take)
    {
        try
        {
            var products = await _productRepository.GetProductsByCollectionNameAsync(collectionName, take);

            var responseDtos = new List<ProductResponseDto>();
            var dtoHelper = new ProductResponseDto();

            foreach (var product in products)
            {
                responseDtos.Add(dtoHelper.mapToProductDto(product));
            }

            var pagedResult = new PagedResult<ProductResponseDto>(responseDtos, responseDtos.Count, 1, take);

            return Result<PagedResult<ProductResponseDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when selecting products by collection {CollectionName}", collectionName);
            return Result<PagedResult<ProductResponseDto>>.Failure("Unable to retrieve collection data");
        }
    }

}
