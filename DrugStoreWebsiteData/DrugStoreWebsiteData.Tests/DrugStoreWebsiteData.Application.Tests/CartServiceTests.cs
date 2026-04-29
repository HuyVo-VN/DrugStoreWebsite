using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using DrugStoreWebSiteData.Application.Services;
using DrugStoreWebSiteData.Domain.Interfaces;
using DrugStoreWebSiteData.Domain.Entities;
using DrugStoreWebSiteData.Application.DTOs.Request;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DrugStoreWebsiteData.Tests.DrugStoreWebsiteData.Application.Tests
{
    public class CartServiceTests
    {
        private readonly Mock<ICartRepository> _mockCartRepo;
        private readonly Mock<IProductRepository> _mockProductRepo;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<CartService>> _mockLogger;
        private readonly CartService _cartService;

        public CartServiceTests()
        {
            _mockCartRepo = new Mock<ICartRepository>();
            _mockProductRepo = new Mock<IProductRepository>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<CartService>>();

            _cartService = new CartService(
                _mockCartRepo.Object,
                _mockLogger.Object,
                _mockProductRepo.Object,
                _mockUnitOfWork.Object
            );
        }

        [Fact]
        public async Task AddToCartAsync_ProductNotFound_ShouldReturnFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new AddToCartRequestDto { ProductId = Guid.NewGuid(), Quantity = 1 };

            _mockProductRepo.Setup(x => x.GetByIdAsync(request.ProductId)).ReturnsAsync((Product?)null);

            // Act
            var result = await _cartService.AddToCartAsync(userId, request);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Contain("Product not found");
            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AddToCartAsync_ProductOutOfStock_ShouldReturnFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Khởi tạo Product thông qua Constructor chuẩn của DDD
            var fakeProduct = new Product(
                name: "Thuốc Test",
                description: "Mô tả",
                price: 10000m,
                stock: 5, // Tồn kho chỉ có 5
                categoryId: Guid.NewGuid(),
                discountPercent: 0,
                discountEndDate: null,
                saleStock: 0,
                specifications: "Specs"
            );

            // Dùng chính Id của fakeProduct gán vào Request (mua 10 cái)
            var request = new AddToCartRequestDto { ProductId = fakeProduct.Id, Quantity = 10 };

            _mockProductRepo.Setup(x => x.GetByIdAsync(request.ProductId)).ReturnsAsync(fakeProduct);

            // Act
            var result = await _cartService.AddToCartAsync(userId, request);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Contain("Quantity reached product limit");
        }

        [Fact]
        public async Task RemoveAsync_Successful_ShouldReturnSuccess()
        {
            // Arrange
            var cartItemId = Guid.NewGuid();
            _mockCartRepo.Setup(x => x.RemoveItemAsync(cartItemId)).ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _cartService.RemoveAsync(cartItemId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Contain("successfully");
            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}