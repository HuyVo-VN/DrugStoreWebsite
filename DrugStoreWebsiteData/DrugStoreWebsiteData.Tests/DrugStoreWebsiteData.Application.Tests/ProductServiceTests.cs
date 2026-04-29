using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using DrugStoreWebSiteData.Application.Services;
using DrugStoreWebSiteData.Domain.Interfaces;
using DrugStoreWebSiteData.Domain.Entities;
using DrugStoreWebSiteData.Application.DTOs.Request;
using System;
using System.Threading;
using System.Threading.Tasks;
using DrugStoreWebSiteData.Application.Interfaces;

namespace DrugStoreWebsiteData.Tests.DrugStoreWebsiteData.Application.Tests
{
    public class ProductServiceTests
    {
        private readonly Mock<IProductRepository> _mockProductRepo;
        private readonly Mock<ICategoryRepository> _mockCategoryRepo;
        private readonly Mock<IPhotoService> _mockPhotoService;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<ProductService>> _mockLogger;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly ProductService _productService;

        public ProductServiceTests()
        {
            _mockProductRepo = new Mock<IProductRepository>();
            _mockCategoryRepo = new Mock<ICategoryRepository>();
            _mockPhotoService = new Mock<IPhotoService>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<ProductService>>();
            _mockCache = new Mock<IDistributedCache>();

            _productService = new ProductService(
                _mockProductRepo.Object,
                _mockCategoryRepo.Object,
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                _mockPhotoService.Object,
                _mockCache.Object
            );
        }

        [Fact]
        public async Task CreateProductAsync_CategoryDoesNotExist_ShouldReturnFailure()
        {
            var request = new CreateProductRequestDto { CategoryId = Guid.NewGuid() };
            _mockCategoryRepo.Setup(x => x.GetByIdAsync(request.CategoryId)).ReturnsAsync((Category?)null);

            var result = await _productService.CreateProductAsync(request);

            result.IsFailure.Should().BeTrue();
            result.Error.Should().Contain("Category does not exist");
            _mockProductRepo.Verify(x => x.AddAsync(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_SuccessfulDeletion_ShouldClearRedisCacheAndReturnSuccess()
        {
            var productId = Guid.NewGuid();
            _mockProductRepo.Setup(x => x.DeleteAsync(productId)).ReturnsAsync(true);

            // FIX LỖI CS0854 TẠI ĐÂY
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _mockCache.Setup(x => x.RemoveAsync(It.IsAny<string>(), CancellationToken.None))
                      .Returns(Task.CompletedTask);

            var result = await _productService.DeleteAsync(productId);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Contain("successfully");

            // FIX LỖI CS0854 TẠI ĐÂY
            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            _mockCache.Verify(x => x.RemoveAsync(It.IsAny<string>(), CancellationToken.None), Times.Exactly(3));
        }

        [Fact]
        public async Task DeleteAsync_ProductNotFound_ShouldReturnFailure()
        {
            var productId = Guid.NewGuid();
            _mockProductRepo.Setup(x => x.DeleteAsync(productId)).ReturnsAsync(false);

            var result = await _productService.DeleteAsync(productId);

            result.IsFailure.Should().BeTrue();
            result.Error.Should().Contain("Error occurred while deleting");

            // FIX LỖI CS0854 TẠI ĐÂY
            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetProductByIdAsync_ProductNotFound_ShouldReturnFailure()
        {
            var productId = Guid.NewGuid();
            _mockProductRepo.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync((Product?)null);

            var result = await _productService.GetProductByIdAsync(productId);

            result.IsFailure.Should().BeTrue();
            result.Error.Should().Contain("Product not found");
        }

        [Fact]
        public async Task UpdateProductAsync_ProductNotFound_ShouldReturnFailure()
        {
            var productId = Guid.NewGuid();
            var request = new UpdateProductRequestDto();

            _mockProductRepo.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync((Product?)null);

            var result = await _productService.UpdateProductAsync(productId, request);

            result.IsFailure.Should().BeTrue();
            result.Error.Should().Contain("Product not found");
            _mockCategoryRepo.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }
    }
}