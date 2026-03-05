using Xunit;
using Moq;
using DrugStoreWebSiteData.Application.DTOs.Request;
using DrugStoreWebSiteData.Application.Services;
using DrugStoreWebSiteData.Domain.Interfaces;
using DrugStoreWebSiteData.Domain.Entities;
using Microsoft.Extensions.Logging;
using DrugStoreWebSiteData.Application.Interfaces;

namespace DrugStoreWebsiteData.Tests.DrugStoreWebsiteData.Application.Tests;
public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _mockProductRepo;
    private readonly Mock<ICategoryRepository> _mockCategoryRepo;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<ProductService>> _mockLogger;
    private readonly Mock<IImageService> _mockImageService;
    private readonly ProductService _productService;

    public ProductServiceTests()
    {
        _mockProductRepo = new Mock<IProductRepository>();
        _mockCategoryRepo = new Mock<ICategoryRepository>();
        
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<ProductService>>();
        _mockImageService = new Mock<IImageService>();

        _productService = new ProductService(
            _mockProductRepo.Object,
            _mockCategoryRepo.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object,
            _mockImageService.Object);
    }

    [Fact]
    public async Task CreateProductAsync_ShouldReturnSuccess_WhenRequestIsValid()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var request = new CreateProductRequestDto
        {
            Name = "Test Product",
            Description = "Test Description",
            Price = 100,
            Stock = 10,
            CategoryId = categoryId
        };

        //simulate existing category
        var mockCategory = new Category("Test Category", "Desc");
        _mockCategoryRepo
            .Setup(repo => repo.GetByIdAsync(categoryId))
            .ReturnsAsync(mockCategory);

        //simulate ProductRepository AddAsync success
        _mockProductRepo
            .Setup(repo => repo.AddAsync(It.IsAny<Product>()))
            .Returns(Task.CompletedTask);

        //simulate unit of work save changes
        _mockUnitOfWork
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        //act
        var result = await _productService.CreateProductAsync(request);

        //assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(request.Name, result.Value.Name);

        //Check that all setups were called
        _mockProductRepo.Verify(repo => repo.AddAsync(It.IsAny<Product>()), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_ShouldReturnFailure_WhenCategoryDoesNotExist()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var request = new CreateProductRequestDto { CategoryId = categoryId, Name = "Test" };

        //simulate non-existing category
        _mockCategoryRepo
            .Setup(repo => repo.GetByIdAsync(categoryId))
            .ReturnsAsync((Category?)null);

        //act
        var result = await _productService.CreateProductAsync(request);

        //assert
        Assert.True(result.IsFailure);
        Assert.Contains("Category does not exist", result.Error);

        //Check that category repo was called, but product repo and unit of work were not
        _mockProductRepo.Verify(repo => repo.AddAsync(It.IsAny<Product>()), Times.Never);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateProductAsync_ShouldReturnFailure_WhenExceptionOccurs()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var request = new CreateProductRequestDto { CategoryId = categoryId, Name = "Test" };
        var mockCategory = new Category("Test", "Desc");

        //simulate exception in category repository
        _mockCategoryRepo
            .Setup(x => x.GetByIdAsync(categoryId))
            .ReturnsAsync(mockCategory);
        
        //simulate db error when SaveChangesAsync is called
        _mockProductRepo
            .Setup(x => x.AddAsync(It.IsAny<Product>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        //act
        var result = await _productService.CreateProductAsync(request);

        //assert
        Assert.True(result.IsFailure);
        Assert.Contains("An error occurred", result.Error);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldReturnSuccess_WhenRequestIsValid()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var request = new UpdateProductRequestDto
        {
            Name = "Updated Product",
            Price = 150,
            Stock = 20,
            CategoryId = categoryId,
            Description = "Updated Desc"
        };

        // Create a mock existing product
        var existingProduct = new Product("Old Name", "Old Desc", 100, 10, Guid.NewGuid());

        var mockCategory = new Category("Test Category", "Desc");

        // simulate existing product and category
        _mockProductRepo.Setup(repo => repo.GetByIdAsync(productId))
            .ReturnsAsync(existingProduct);

        // simulate existing category
        _mockCategoryRepo.Setup(repo => repo.GetByIdAsync(request.CategoryId))
            .ReturnsAsync(mockCategory);

        // simulate Repository.Update thành công
        _mockProductRepo.Setup(repo => repo.Update(existingProduct));

        // simulate SaveChangesAsync thành công
        _mockUnitOfWork.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _productService.UpdateProductAsync(productId, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Updated Product", result.Value.Name);

        // check that all setups were called
        _mockProductRepo.Verify(repo => repo.Update(existingProduct), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldReturnFailure_WhenProductNotFound()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new UpdateProductRequestDto();

        // simulate ProductRepository KHÔNG TÌM THẤY sản phẩm
        _mockProductRepo.Setup(repo => repo.GetByIdAsync(productId))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _productService.UpdateProductAsync(productId, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Product not found", result.Error);

        // ensure other calls were not made
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldReturnFailure_WhenCategoryNotFound()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new UpdateProductRequestDto { CategoryId = Guid.NewGuid() };
        var existingProduct = new Product("Old Name", "Old Desc", 100, 10, Guid.NewGuid());

        // simulate ProductRepository finds the product
        _mockProductRepo.Setup(repo => repo.GetByIdAsync(productId))
            .ReturnsAsync(existingProduct);

        // simulate CategoryRepository does NOT find the category
        _mockCategoryRepo.Setup(repo => repo.GetByIdAsync(request.CategoryId))
            .ReturnsAsync((Category?)null);

        // Act
        var result = await _productService.UpdateProductAsync(productId, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Category not found", result.Error);

        // ensure Update and SaveChanges were not called
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldReturnFailure_WhenSaveChangesFails()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var request = new UpdateProductRequestDto { CategoryId = categoryId, Name = "Test" };
        var existingProduct = new Product("Old Name", "Old Desc", 100, 10, Guid.NewGuid());
        var mockCategory = new Category("Test Category", "Desc");

        // simulate ProductRepository finds the product
        _mockProductRepo.Setup(repo => repo.GetByIdAsync(productId))
            .ReturnsAsync(existingProduct);

        // simulate CategoryRepository finds the category
        _mockCategoryRepo.Setup(repo => repo.GetByIdAsync(request.CategoryId))
            .ReturnsAsync(mockCategory);

        // simulate Repository.Update
        _mockProductRepo.Setup(repo => repo.Update(existingProduct));

        // simulate SaveChangesAsync throwing exception
        _mockUnitOfWork.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
        .ThrowsAsync(new Exception("Simulated DB Error"));

        // Act
        var result = await _productService.UpdateProductAsync(productId, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Simulated DB Error", result.Error);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnSuccess_WhenDeleteIsSuccessful()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _mockProductRepo
            .Setup(repo => repo.DeleteAsync(productId))
            .ReturnsAsync(true);

        _mockUnitOfWork
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _productService.DeleteAsync(productId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Product deleted successfully", result.Value);

        _mockProductRepo.Verify(repo => repo.DeleteAsync(productId), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(default), Times.Once);
    }
    [Fact]
    public async Task DeleteAsync_ShouldReturnFailure_WhenRepositoryReturnsFalse()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _mockProductRepo
            .Setup(repo => repo.DeleteAsync(productId))
            .ReturnsAsync(false);

        // Act
        var result = await _productService.DeleteAsync(productId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Error occurred while deleting product", result.Error);

        _mockProductRepo.Verify(repo => repo.DeleteAsync(productId), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(default), Times.Never);
    }
    [Fact]
    public async Task DeleteAsync_ShouldReturnFailure_WhenExceptionIsThrown()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _mockProductRepo
            .Setup(repo => repo.DeleteAsync(productId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _productService.DeleteAsync(productId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("An error occurred while deleting the product", result.Error);

        _mockProductRepo.Verify(repo => repo.DeleteAsync(productId), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(default), Times.Never);
    }
}