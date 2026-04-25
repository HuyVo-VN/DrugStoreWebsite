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
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IPhotoService _photoService;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProductService> _logger;
    private readonly IDistributedCache _cache;

    public ProductService(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        ILogger<ProductService> logger,
        IPhotoService photoService,
        IDistributedCache cache)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _photoService = photoService;
        _cache = cache;
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
                var newImageUrl = await _photoService.AddPhotoAsync(request.ImageFile);

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
                    product.SaleStock,
                    product.Specifications
                );
            }

            // Add product to repository
            await _productRepository.AddAsync(product);

            // Save changes
            await _unitOfWork.SaveChangesAsync();

            // Return the created product as ProductDto
            var responseDto = new ProductResponseDto();
            await _cache.RemoveAsync("products_all_p1_s15");
            await _cache.RemoveAsync("products_sale_p1_s10");
            await _cache.RemoveAsync("products_bestsellers_p1_s10");
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
                await _cache.RemoveAsync("products_all_p1_s15");
                await _cache.RemoveAsync("products_sale_p1_s10");
                await _cache.RemoveAsync("products_bestsellers_p1_s10");
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
                await _cache.RemoveAsync("products_all_p1_s15");
                await _cache.RemoveAsync("products_sale_p1_s10");
                await _cache.RemoveAsync("products_bestsellers_p1_s10");
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
                    await _photoService.DeletePhotoAsync(productResult.ImageUrl);
                }
                // save new picture
                updatedImageUrl = await _photoService.AddPhotoAsync(request.ImageFile);
            }
            else if (request.DeleteCurrentImage)
            {
                if (!string.IsNullOrEmpty(productResult.ImageUrl))
                {
                    await _photoService.DeletePhotoAsync(productResult.ImageUrl);
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
                request.SaleStock,
                request.Specifications);

            _productRepository.Update(productResult);

            await _unitOfWork.SaveChangesAsync();

            await _cache.RemoveAsync("products_all_p1_s15");
            await _cache.RemoveAsync("products_sale_p1_s10");
            await _cache.RemoveAsync("products_bestsellers_p1_s10");
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
            // Tên thẻ kho có kèm số trang để không bị lộn
            string cacheKey = $"products_all_p{pageNumber}_s{pageSize}";

            // Lục kho
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.LogInformation($"[CACHE HIT] Lấy All Products {cacheKey} từ Redis.");
                var cachedResult = JsonSerializer.Deserialize<PagedResult<ProductResponseDto>>(cachedData);
                return Result<PagedResult<ProductResponseDto>>.Success(cachedResult);
            }

            // Kho trống -> Xuống DB
            var (products, totalCount) = await _productRepository.GetPagedAsync(pageNumber, pageSize);
            if (products == null) return Result<PagedResult<ProductResponseDto>>.Failure("Product list not found");

            var dtoHelper = new ProductResponseDto();
            var responseDtos = products.Select(p => dtoHelper.mapToProductDto(p)).ToList();
            var pagedResult = new PagedResult<ProductResponseDto>(responseDtos, totalCount, pageNumber, pageSize);

            // Cất kho (Lưu 1 tiếng)
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(pagedResult), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });

            return Result<PagedResult<ProductResponseDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving all products");
            return Result<PagedResult<ProductResponseDto>>.Failure($"An error occurred: {ex.Message}");
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

    public async Task<Result<string>> CancelSaleAsync(Guid productId)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(productId);

            if (product == null)
            {
                return Result<string>.Failure("Can not find any products.");
            }

            product.CancelFlashSale();

            _productRepository.Update(product);

            await _unitOfWork.SaveChangesAsync();

            return Result<string>.Success("Flash Sale has been successfully turned off.!");
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Error when turning off sale: {ex.Message}");
        }
    }

    public async Task<Result<PagedResult<ProductResponseDto>>> GetSaleProductsPagedAsync(int pageIndex, int pageSize)
    {
        try
        {
            string cacheKey = $"products_sale_p{pageIndex}_s{pageSize}";

            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.LogInformation($"[CACHE HIT] Lấy Sale Products {cacheKey} từ Redis.");
                return Result<PagedResult<ProductResponseDto>>.Success(JsonSerializer.Deserialize<PagedResult<ProductResponseDto>>(cachedData));
            }

            var (items, totalCount) = await _productRepository.GetSaleProductsPagedAsync(pageIndex, pageSize);
            var dtos = items.Select(p => new ProductResponseDto().mapToProductDto(p)).ToList();
            var pagedResult = new PagedResult<ProductResponseDto>(dtos, totalCount, pageIndex, pageSize);

            // Flash sale thay đổi nhanh, chỉ cache 30 phút
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(pagedResult), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) });

            return Result<PagedResult<ProductResponseDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            return Result<PagedResult<ProductResponseDto>>.Failure($"Error when loading Flash Sale: {ex.Message}");
        }
    }
    public async Task<Result<PagedResult<ProductResponseDto>>> GetBestSellersPagedAsync(int pageIndex, int pageSize)
    {
        try
        {
            string cacheKey = $"products_bestsellers_p{pageIndex}_s{pageSize}";

            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.LogInformation($"[CACHE HIT] Lấy Best Sellers {cacheKey} từ Redis.");
                return Result<PagedResult<ProductResponseDto>>.Success(JsonSerializer.Deserialize<PagedResult<ProductResponseDto>>(cachedData));
            }

            var (items, totalCount) = await _productRepository.GetBestSellersPagedAsync(pageIndex, pageSize);
            var dtos = items.Select(p => new ProductResponseDto().mapToProductDto(p)).ToList();
            var pagedResult = new PagedResult<ProductResponseDto>(dtos, totalCount, pageIndex, pageSize);

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(pagedResult), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });

            return Result<PagedResult<ProductResponseDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            return Result<PagedResult<ProductResponseDto>>.Failure($"Error when loading Best Sellers: {ex.Message}");
        }
    }

    public async Task<Result<PagedResult<ProductResponseDto>>> GetProductsByCollectionPagedAsync(Guid collectionId, int pageIndex, int pageSize)
    {
        try
        {
            // Key này phải kẹp thêm cái ID của collection vào để không bị trùng data
            string cacheKey = $"products_col_{collectionId}_p{pageIndex}_s{pageSize}";

            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.LogInformation($"[CACHE HIT] Lấy Collection Products {cacheKey} từ Redis.");
                return Result<PagedResult<ProductResponseDto>>.Success(JsonSerializer.Deserialize<PagedResult<ProductResponseDto>>(cachedData));
            }

            var (items, totalCount) = await _productRepository.GetProductsByCollectionPagedAsync(collectionId, pageIndex, pageSize);
            var dtos = items.Select(p => new ProductResponseDto().mapToProductDto(p)).ToList();
            var pagedResult = new PagedResult<ProductResponseDto>(dtos, totalCount, pageIndex, pageSize);

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(pagedResult), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });

            return Result<PagedResult<ProductResponseDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            return Result<PagedResult<ProductResponseDto>>.Failure($"Error when loading Collection: {ex.Message}");
        }
    }

}
