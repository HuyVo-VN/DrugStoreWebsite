using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using DrugStoreWebSiteData.Infrastructure.Services;
using DrugStoreWebSiteData.Domain.Interfaces;
using DrugStoreWebSiteData.Domain.Entities;
using DrugStoreWebSiteData.Application.DTOs.Request;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DrugStoreWebsiteData.Tests.DrugStoreWebsiteData.Application.Tests
{
    public class OrderServiceTests
    {
        private readonly Mock<IOrderRepository> _mockOrderRepo;
        private readonly Mock<IProductRepository> _mockProductRepo;
        private readonly Mock<ILogger<OrderService>> _mockLogger;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly OrderService _orderService;

        public OrderServiceTests()
        {
            _mockOrderRepo = new Mock<IOrderRepository>();
            _mockProductRepo = new Mock<IProductRepository>();
            _mockLogger = new Mock<ILogger<OrderService>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();

            _orderService = new OrderService(
                _mockOrderRepo.Object,
                _mockProductRepo.Object,
                _mockLogger.Object,
                _mockUnitOfWork.Object
            );
        }

        [Fact]
        public async Task CreateOrderAsync_ProductNotFound_ShouldReturnFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new CreateOrderRequestDto
            {
                TotalAmount = 100000,
                ShippingAddress = "123 Test St",
                PhoneNumber = "0123456789",
                Items = new List<CreateOrderItemRequestDto>
                {
                    new CreateOrderItemRequestDto { ProductId = Guid.NewGuid(), Quantity = 2 }
                }
            };

            _mockProductRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Product?)null);

            // Act
            var result = await _orderService.CreateOrderAsync(userId, request);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Contain("Product not found");
        }

        [Fact]
        public async Task CreateOrderAsync_NotEnoughStock_ShouldReturnFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Khởi tạo Product thông qua Constructor chuẩn của DDD
            var fakeProduct = new Product(
                name: "Panadol",
                description: "Mô tả",
                price: 10000m,
                stock: 2, // Tồn kho chỉ có 2 hộp
                categoryId: Guid.NewGuid(),
                discountPercent: 0,
                discountEndDate: null,
                saleStock: 0,
                specifications: "Specs"
            );

            var request = new CreateOrderRequestDto
            {
                TotalAmount = 100000,
                ShippingAddress = "123 Test St",
                PhoneNumber = "0123456789",
                Items = new List<CreateOrderItemRequestDto>
                {
                    // Đặt mua 10 hộp
                    new CreateOrderItemRequestDto { ProductId = fakeProduct.Id, Quantity = 10 }
                }
            };

            _mockProductRepo.Setup(x => x.GetByIdAsync(fakeProduct.Id)).ReturnsAsync(fakeProduct);

            // Act
            var result = await _orderService.CreateOrderAsync(userId, request);

            // Assert
            result.IsFailure.Should().BeTrue();
            // Lỗi trả về phải báo hết hàng
            result.Error.Should().Contain("out of stock");
        }

        [Fact]
        public async Task DeleteOrderAsync_Successful_ShouldReturnSuccess()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            _mockOrderRepo.Setup(x => x.DeleteAsync(orderId)).ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _orderService.DeleteOrderAsync(orderId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Contain("successfully");
            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}