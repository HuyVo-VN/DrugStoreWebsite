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

public class CollectionService : ICollectionService
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly IImageService _imageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CollectionService> _logger;

    public CollectionService(
        ICollectionRepository collectionRepository,
        IUnitOfWork unitOfWork,
        ILogger<CollectionService> logger,
        IImageService imageService)
    {
        _collectionRepository = collectionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _imageService = imageService;
    }

    public async Task<Result<IEnumerable<CollectionWithProductsResponseDto>>> GetHomepageCollectionsAsync(int productLimit = 5)
    {
        try
        {
            var collections = await _collectionRepository.GetHomepageCollectionsAsync();
            var resultList = new List<CollectionWithProductsResponseDto>();
            var dtoHelper = new ProductResponseDto();

            foreach (var collection in collections)
            {
                var activeProducts = collection.ProductCollections
                    .Where(pc => pc.Product.IsActive)
                    .Select(pc => pc.Product)
                    .Take(productLimit)
                    .ToList();

                if (activeProducts.Any())
                {
                    var collectionDto = new CollectionWithProductsResponseDto
                    {
                        Id = collection.Id,
                        Name = collection.Name,
                        Description = collection.Description,
                        DisplayOrder = collection.DisplayOrder,
                        Products = activeProducts.Select(p => dtoHelper.mapToProductDto(p)).ToList()
                    };
                    resultList.Add(collectionDto);
                }
            }

            return Result<IEnumerable<CollectionWithProductsResponseDto>>.Success(resultList);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CollectionWithProductsResponseDto>>.Failure("Error retrieving data from the homepage Collection");
        }
    }

    public async Task<Result<IEnumerable<CollectionWithProductsResponseDto>>> GetAllCollectionsAdminAsync()
    {
        try
        {
            var collections = await _collectionRepository.GetAllCollectionsAdminAsync();
            var dtos = collections.Select(c => new CollectionWithProductsResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                DisplayOrder = c.DisplayOrder,
                ShowOnHomePage = c.ShowOnHomePage,
                IsActive = c.IsActive,
                ProductIds = c.ProductCollections.Select(pc => pc.ProductId).ToList()
            }).ToList();

            return Result<IEnumerable<CollectionWithProductsResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving the list of all Collections for Admin");
            return Result<IEnumerable<CollectionWithProductsResponseDto>>.Failure("Unable to retrieve the Collection list");
        }
    }

    public async Task<Result<string>> CreateCollectionAsync(CreateCollectionRequestDto request)
    {
        try
        {
            var collection = new Collection
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                ShowOnHomePage = request.ShowOnHomePage,
                DisplayOrder = request.DisplayOrder,
                IsActive = request.IsActive
            };

            await _collectionRepository.AddAsync(collection);

            await _collectionRepository.UpdateCollectionProductsAsync(collection.Id, request.ProductIds);

            await _unitOfWork.SaveChangesAsync();

            return Result<string>.Success("Collection created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when creating a new Collection");
            return Result<string>.Failure("System error when creating a Collection");
        }
    }

    public async Task<Result<string>> UpdateCollectionAsync(Guid id, UpdateCollectionRequestDto request)
    {
        try
        {
            var collection = await _collectionRepository.GetByIdAsync(id);
            if (collection == null)
            {
                return Result<string>.Failure("This Collection was not found.");
            }

            collection.Name = request.Name;
            collection.Description = request.Description;
            collection.IsActive = request.IsActive;
            collection.ShowOnHomePage = request.ShowOnHomePage;
            collection.DisplayOrder = request.DisplayOrder;

            _collectionRepository.Update(collection);

            await _collectionRepository.UpdateCollectionProductsAsync(id, request.ProductIds);

            await _unitOfWork.SaveChangesAsync();

            return Result<string>.Success("Collection updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Collection ID: {Id}", id);
            return Result<string>.Failure("System error when updating Collection");
        }
    }

    public async Task<Result<string>> DeleteCollectionAsync(Guid id)
    {
        try
        {
            var result = await _collectionRepository.DeleteAsync(id);
            if (!result)
            {
                return Result<string>.Failure("No Collection found to delete");
            }

            await _unitOfWork.SaveChangesAsync();
            return Result<string>.Success("Delete Collection successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when deleting Collection ID: {Id}", id);
            return Result<string>.Failure("System error when deleting Collection");
        }
    }
}