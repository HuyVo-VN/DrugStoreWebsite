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

namespace DrugStoreWebsiteData.Tests.DrugStoreWebsiteData.Application.Tests
{
    public class CategoryServiceTests
    {
        private readonly Mock<ICategoryRepository> _mockCategoryRepo;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<CategoryService>> _mockLogger;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly CategoryService _categoryService;

        public CategoryServiceTests()
        {
            _mockCategoryRepo = new Mock<ICategoryRepository>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<CategoryService>>();
            _mockCache = new Mock<IDistributedCache>();

            _categoryService = new CategoryService(
                _mockCategoryRepo.Object,
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                _mockCache.Object
            );
        }

        [Fact]
        public async Task CreateCategoryAsync_ShouldReturnSuccess()
        {
            // Arrange
            var request = new CreateCategoryRequestDto { Name = "Thuốc kháng sinh", Description = "Desc" };
            var currentUser = "HuyVo";

            // FIX LỖI CS0854: Thêm It.IsAny<CancellationToken>()
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockCache.Setup(x => x.RemoveAsync(It.IsAny<string>(), CancellationToken.None)).Returns(Task.CompletedTask);

            // Act
            var result = await _categoryService.CreateCategoryAsync(request, currentUser);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Name.Should().Be("Thuốc kháng sinh");
            _mockCategoryRepo.Verify(x => x.AddAsync(It.IsAny<Category>()), Times.Once);
            _mockCache.Verify(x => x.RemoveAsync("categories_all", CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task GetCategoryByIdAsync_NotFound_ShouldReturnFailure()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockCategoryRepo.Setup(x => x.GetByIdAsync(id)).ReturnsAsync((Category?)null);

            // Act
            var result = await _categoryService.GetCategoryByIdAsync(id);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Contain("not found");
        }
    }
}