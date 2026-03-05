using DrugStoreWebSiteData.Application.DTOs.Request;
using DrugStoreWebSiteData.Application.DTOs.Response;

using DrugStoreWebSiteData.Application.Common;
using DrugStoreWebSiteData.Application.DTOs;

namespace DrugStoreWebSiteData.Application.Interfaces;

public interface IProductService
{
    Task<Result<string>> DeleteAsync(Guid productId);
    Task<Result<string>> UpdateStatusAsync(UpdateStatusProductRequestDto requestDto);
    Task<Result<ProductResponseDto>> CreateProductAsync(CreateProductRequestDto request);
    Task<Result<ProductResponseDto>> GetProductByIdAsync(Guid id);
    Task<Result<ProductResponseDto>> UpdateProductAsync(Guid id, UpdateProductRequestDto request);
    Task<Result<PagedResult<ProductResponseDto>>> GetAllProductsAsync(int pageNumber, int pageSize);
    Task<Result<PagedResult<ProductResponseDto>>> SearchProductsAsync(SearchProductRequestDto requestDto);
    Task<Result<PagedResult<ProductResponseDto>>> FilterProductsAsync(FilterProductRequestDto requestDto);
    Task<Result<PagedResult<ProductResponseDto>>> GetSaleProductsAsync(int limit = 10);
    Task<Result<PagedResult<ProductResponseDto>>> GetBestSellerProductsAsync(int limit = 10);
    Task<Result<PagedResult<ProductResponseDto>>> GetProductsByCollectionNameAsync(string collectionName, int take);
}
