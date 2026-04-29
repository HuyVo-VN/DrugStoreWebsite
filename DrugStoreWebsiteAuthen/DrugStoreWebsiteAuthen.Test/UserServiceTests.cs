using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using DrugStoreWebsiteAuthen.Application.Services;
using DrugStoreWebsiteAuthen.Domain;
using DrugStoreWebsiteAuthen.Application.Interfaces;
using DrugStoreWebsiteAuthen.Application.Common;
using System.Threading.Tasks;

namespace DrugStoreWebsiteAuthen.Test
{
    public class UserServiceTests
    {
        // 1. Khai báo các đối tượng Mock (Giả lập)
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
        private readonly Mock<IEmailSender> _mockEmailSender;
        private readonly Mock<ILogger<UserService>> _mockLogger;

        // 2. Khai báo Service thật cần test
        private readonly UserService _userService;

        public UserServiceTests()
        {
            // Khởi tạo các bản sao giả
            _mockUserRepository = new Mock<IUserRepository>();
            _mockUserManager = MockUserManager();
            _mockRoleManager = MockRoleManager();
            _mockEmailSender = new Mock<IEmailSender>();
            _mockLogger = new Mock<ILogger<UserService>>();

            // Bơm các bản sao giả vào Service thật
            _userService = new UserService(
                _mockUserRepository.Object,
                _mockUserManager.Object,
                _mockRoleManager.Object,
                _mockEmailSender.Object,
                _mockLogger.Object
            );
        }

        #region Kịch bản 1: Test hàm ValidateUserAsync (Đăng nhập)

        [Fact]
        public async Task ValidateUserAsync_WithValidCredentials_ShouldReturnTrue()
        {
            // Arrange (Chuẩn bị dữ liệu)
            var userName = "testuser";
            var password = "ValidPassword123!";
            var fakeUser = new User { UserName = userName };

            // Dạy cho thằng giả lập biết phải làm gì khi được gọi
            _mockUserManager.Setup(x => x.FindByNameAsync(userName))
                            .ReturnsAsync(fakeUser); // Trả về user giả
            
            _mockUserManager.Setup(x => x.CheckPasswordAsync(fakeUser, password))
                            .ReturnsAsync(true); // Trả về true (pass đúng)

            // Act (Thực thi hàm thật)
            var result = await _userService.ValidateUserAsync(userName, password);

            // Assert (Kiểm chứng kết quả bằng FluentAssertions)
            result.Should().BeTrue(); // Phải là true
        }

        [Fact]
        public async Task ValidateUserAsync_WithWrongPassword_ShouldReturnFalse()
        {
            // Arrange
            var userName = "testuser";
            var password = "WrongPassword!";
            var fakeUser = new User { UserName = userName };

            _mockUserManager.Setup(x => x.FindByNameAsync(userName)).ReturnsAsync(fakeUser);
            _mockUserManager.Setup(x => x.CheckPasswordAsync(fakeUser, password)).ReturnsAsync(false); // Sai pass

            // Act
            var result = await _userService.ValidateUserAsync(userName, password);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateUserAsync_UserNotFound_ShouldReturnFalse()
        {
            // Arrange
            var userName = "unknown_user";
            var password = "AnyPassword";

            // Dạy Mock trả về null (không tìm thấy)
            _mockUserManager.Setup(x => x.FindByNameAsync(userName)).ReturnsAsync((User)null);

            // Act
            var result = await _userService.ValidateUserAsync(userName, password);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region Kịch bản 2: Test hàm GetUserByUserNameAsync

        [Fact]
        public async Task GetUserByUserNameAsync_UserExists_ShouldReturnSuccessResult()
        {
            // Arrange
            var userName = "testuser";
            var fakeUser = new User { UserName = userName, Email = "test@gmail.com" };

            _mockUserManager.Setup(x => x.FindByNameAsync(userName)).ReturnsAsync(fakeUser);

            // Act
            var result = await _userService.GetUserByUserNameAsync(userName);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Status.Should().Be(ResultStatus.Success);
            result.Data.Should().NotBeNull();
            result.Data.Email.Should().Be("test@gmail.com");
        }

        [Fact]
        public async Task GetUserByUserNameAsync_UserDoesNotExist_ShouldReturnNotFoundResult()
        {
            // Arrange
            var userName = "ghostuser";
            _mockUserManager.Setup(x => x.FindByNameAsync(userName)).ReturnsAsync((User)null);

            // Act
            var result = await _userService.GetUserByUserNameAsync(userName);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Status.Should().Be(ResultStatus.NotFound);
            result.Message.Should().Contain("not found");
        }

        #endregion

        #region Kịch bản 3: Test hàm RegisterUserAsync

        [Fact]
        public async Task RegisterUserAsync_SuccessfulCreation_ShouldReturnStatus200()
        {
            // Arrange
            var newUser = new User { UserName = "newuser", Email = "new@gmail.com" };
            var password = "StrongPassword123!";

            _mockUserManager.Setup(x => x.CreateAsync(newUser, password))
                            .ReturnsAsync(IdentityResult.Success); // Tạo user thành công
            
            _mockUserManager.Setup(x => x.AddToRoleAsync(newUser, "Customer"))
                            .ReturnsAsync(IdentityResult.Success); // Gán quyền thành công

            // Act
            var result = await _userService.RegisterUserAsync(newUser, password);

            // Assert
            result.Status.Should().Be(200);
            result.Data.Should().Be(newUser);
        }

        [Fact]
        public async Task RegisterUserAsync_CreationFails_ShouldReturnStatus400()
        {
            // Arrange
            var newUser = new User { UserName = "newuser" };
            var password = "weak";
            var identityErrors = IdentityResult.Failed(new IdentityError { Description = "Password too weak" });

            _mockUserManager.Setup(x => x.CreateAsync(newUser, password))
                            .ReturnsAsync(identityErrors); // Tạo user thất bại

            // Act
            var result = await _userService.RegisterUserAsync(newUser, password);

            // Assert
            result.Status.Should().Be(400);
            result.Message.Should().Contain("failed");
            // Đảm bảo AddToRole KHÔNG bao giờ được gọi vì Create đã tạch
            _mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never); 
        }

        #endregion

        #region Helpers để Mock UserManager & RoleManager (Rất quan trọng)
        
        private static Mock<UserManager<User>> MockUserManager()
        {
            var store = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
        }

        private static Mock<RoleManager<IdentityRole>> MockRoleManager()
        {
            var store = new Mock<IRoleStore<IdentityRole>>();
            return new Mock<RoleManager<IdentityRole>>(store.Object, null, null, null, null);
        }

        #endregion
    }
}