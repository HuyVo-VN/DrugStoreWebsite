using Xunit;
using Moq;
using DrugStoreWebSiteData.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FluentAssertions;

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
        public async Task SaveImageAsync_WhenFileIsNull_ShouldThrowArgumentException()
        {
            // Arrange
            Microsoft.AspNetCore.Http.IFormFile? file = null;
            string folderName = "test-folder";

            // Act
            // Đóng gói hành động gọi hàm vào một Func để FluentAssertions bắt lỗi
            Func<Task> action = async () => await _imageService.SaveImageAsync(file!, folderName);

            // Assert
            // Kịch bản đúng: Phải ném ra lỗi ArgumentException
            await action.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task SaveImageAsync_WhenFileLengthIsZero_ShouldThrowArgumentException()
        {
            // Arrange
            var mockFile = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
            mockFile.Setup(f => f.Length).Returns(0); // Giả lập file có dung lượng = 0
            string folderName = "test-folder";

            // Act
            Func<Task> action = async () => await _imageService.SaveImageAsync(mockFile.Object, folderName);

            // Assert
            // Kịch bản đúng: Phải ném ra lỗi ArgumentException
            await action.Should().ThrowAsync<ArgumentException>();
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