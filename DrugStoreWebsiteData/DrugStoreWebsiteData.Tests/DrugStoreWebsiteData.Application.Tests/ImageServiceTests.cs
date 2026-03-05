using Xunit;
using Moq;
using DrugStoreWebSiteData.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DrugStoreWebSiteData.Application.Tests
{
    public class ImageServiceTests
    {
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly ImageService _imageService;
        private readonly string _tempPath;
        private readonly Mock<ILogger<ImageService>> _mockLogger;

        public ImageServiceTests()
        {
            _mockEnvironment = new Mock<IWebHostEnvironment>();
            _mockLogger = new Mock<ILogger<ImageService>>();

            // 1. create temp folder to test
            _tempPath = Path.Combine(Path.GetTempPath(), "DrugStoreTest_Images");
            if (!Directory.Exists(_tempPath))
            {
                Directory.CreateDirectory(_tempPath);
            }

            // simulate webhost point at this folder
            _mockEnvironment.Setup(m => m.WebRootPath).Returns(_tempPath);

            _imageService = new ImageService(_mockEnvironment.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task SaveImageAsync_ShouldReturnNull_WhenFileIsNull()
        {
            // Act
            var result = await _imageService.SaveImageAsync(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SaveImageAsync_ShouldReturnNull_WhenFileLengthIsZero()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(0);

            // Act
            var result = await _imageService.SaveImageAsync(mockFile.Object);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void DeleteImage_ShouldNotThrowError_WhenPathIsInvalid()
        {
            // Test if delete funtion crash when the URL wrong
            // Act & Assert
            var exception = Record.Exception(() => _imageService.DeleteImage("duong/dan/tao/lao.jpg"));
            Assert.Null(exception); // Not to throw error
        }
        
        // Clean temp folder when done (Destructor)
        ~ImageServiceTests()
        {
            if (Directory.Exists(_tempPath))
            {
                try { Directory.Delete(_tempPath, true); } catch { }
            }
        }
    }
}