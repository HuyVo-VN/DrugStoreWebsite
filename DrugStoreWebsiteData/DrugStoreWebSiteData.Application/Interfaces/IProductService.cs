using DrugStoreWebSiteData.Application.DTOs.Request;
using DrugStoreWebSiteData.Application.DTOs.Response;

using DrugStoreWebSiteData.Application.Common;
using DrugStoreWebSiteData.Application.DTOs;
using DrugStoreWebSiteData.Domain.Entities;

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
    Task<Result<PagedResult<ProductResponseDto>>> GetSaleProductsPagedAsync(int pageIndex, int pageSize);
    Task<Result<PagedResult<ProductResponseDto>>> GetBestSellersPagedAsync(int pageIndex, int pageSize);
    Task<Result<PagedResult<ProductResponseDto>>> GetProductsByCollectionPagedAsync(Guid collectionId, int pageIndex, int pageSize);
    Task<Result<string>> CancelSaleAsync(Guid productId);
}
