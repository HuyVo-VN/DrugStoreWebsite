using DrugStoreWebSiteData.Application.DTOs.Request;
using DrugStoreWebSiteData.Application.DTOs.Response;

using DrugStoreWebSiteData.Application.Common;
using DrugStoreWebSiteData.Application.DTOs;

namespace DrugStoreWebSiteData.Application.Interfaces;

public interface ICollectionService
{
    Task<Result<IEnumerable<CollectionWithProductsResponseDto>>> GetHomepageCollectionsAsync(int productLimit = 5);
    Task<Result<IEnumerable<CollectionWithProductsResponseDto>>> GetAllCollectionsAdminAsync();
    Task<Result<string>> CreateCollectionAsync(CreateCollectionRequestDto request);
    Task<Result<string>> UpdateCollectionAsync(Guid id, UpdateCollectionRequestDto request);
    Task<Result<string>> DeleteCollectionAsync(Guid id);
}