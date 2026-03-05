using Xunit;
using Moq;
using DrugStoreWebSiteData.Application.DTOs.Request;
using DrugStoreWebSiteData.Domain.Interfaces;
using DrugStoreWebSiteData.Domain.Entities;
using DrugStoreWebSiteData.Application.Services;
using Microsoft.Extensions.Logging;

namespace DrugStoreWebsite.Application.Tests
{
    public class CategoryServiceTests
    {
        // Mock Object
        private readonly Mock<ICategoryRepository> _mockCategoryRepo;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<CategoryService>> _mockLogger;

        // Service
        private readonly CategoryService _categoryService;

        public CategoryServiceTests()
        {
            // Create Mock Objects
            _mockCategoryRepo = new Mock<ICategoryRepository>();
            
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<CategoryService>>();

            // Create instance of CategoryService with Mocks
            _categoryService = new CategoryService(
                _mockLogger.Object,
                _mockCategoryRepo.Object,
                _mockUnitOfWork.Object
            );
        }

        [Fact]
        public async Task CreateCategoryAsync_ShouldReturnSuccess_WhenRequestIsValid()
        {
            // Arrange 
            var request = new CreateCategoryRequestDto
            {
                Name = "New Category",
                Description = "Description of new category"
            };

            // Simulate no existing category found
            _mockCategoryRepo
                .Setup(repo => repo.GetByNameAsync(request.Name))
                .ReturnsAsync((Category?)null);

            // Simulate AddAsync success
            _mockCategoryRepo
                .Setup(repo => repo.AddAsync(It.IsAny<Category>()))
                .Returns(Task.CompletedTask);

            // Simulate SaveChangesAsync success
            _mockUnitOfWork
                .Setup(uow => uow.SaveChangesAsync(default))
                .ReturnsAsync(1);

            // Act
            var result = await _categoryService.CreateCategoryAsync(request);
            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task CreateCategoryAsync_ShouldReturnFailure_WhenNameAlreadyExists()
        {
            // Arrange
            var request = new CreateCategoryRequestDto { Name = "Existing Category" };
            var existingCategory = new Category(request.Name, "Desc");

            // simulate find existing name
            _mockCategoryRepo
                .Setup(repo => repo.GetByNameAsync(request.Name))
                .ReturnsAsync(existingCategory);

            // Act
            var result = await _categoryService.CreateCategoryAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("already exists", result.Error);

            // Verify NOT called
            _mockCategoryRepo.Verify(repo => repo.AddAsync(It.IsAny<Category>()), Times.Never);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        }

        [Fact]
        public async Task CreateCategoryAsync_ShouldReturnFailure_WhenSaveChangesFails()
        {
            // Arrange
            var request = new CreateCategoryRequestDto
            {
                Name = "New Category"
            };

            // Simulate no existing category found
            _mockCategoryRepo
                .Setup(repo => repo.GetByNameAsync(request.Name))
                .ReturnsAsync((Category?)null);
            // Simulate AddAsync success
            _mockCategoryRepo
                .Setup(repo => repo.AddAsync(It.IsAny<Category>()))
                .Returns(Task.CompletedTask);
            // Simulate SaveChangesAsync failure
            _mockUnitOfWork
                .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("DB Error"));
            // Act
            var result = await _categoryService.CreateCategoryAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("DB Error", result.Error);
        }
    }
}