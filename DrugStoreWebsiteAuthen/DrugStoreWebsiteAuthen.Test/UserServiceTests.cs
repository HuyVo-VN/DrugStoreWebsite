using System.Threading.Tasks;
using DrugStoreWebsiteAuthen.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using System.Collections.Generic;
using System.Net;

namespace DrugStoreWebsiteAuthen.Tests
{
    // Simulated classes for testing
    //-----------------------------------------------------

    //UserManager and RoleManager is a concrete class, we cannot mock them directly. 
    // We have to mock their dependencies (IUserStore, IRoleStore).

    // Helper to create Mock UserManager
    public static class MockUserManager
    {
        public static Mock<UserManager<User>> Create()
        {
            var userStoreMock = new Mock<IUserStore<User>>();

            return new Mock<UserManager<User>>(
                userStoreMock.Object,
                null, // IOptions<IdentityOptions>
                null, // IPasswordHasher<User>
                null, // IEnumerable<IUserValidator<User>>
                null, // IEnumerable<IPasswordValidator<User>>
                null, // ILookupNormalizer
                null, // IdentityErrorDescriber
                null, // IServiceProvider
                null  // ILogger<UserManager<User>>
            );
        }
    }

    // Helper to create Mock RoleManager
    public static class MockRoleManager
    {
        public static Mock<RoleManager<IdentityRole>> Create()
        {
            var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();

            return new Mock<RoleManager<IdentityRole>>(
                roleStoreMock.Object,
                null, // IEnumerable<IRoleValidator<IdentityRole>>
                null, // ILookupNormalizer
                null, // IdentityErrorDescriber
                null  // ILogger<RoleManager<IdentityRole>>
            );
        }
    }

    //-----------------------------------------------------
    // UNIT TESTS FOR UserService
    //-----------------------------------------------------

    public class UserServiceTests
    {
        // Dependencies (Mocks)
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
        private readonly Mock<IEmailSender> _mockEmailSender;
        private readonly Mock<ILogger<UserService>> _mockLogger;


        // system Under Test (SUT)
        private readonly UserService _userService;

        public UserServiceTests()
        {
            // initialize simple mocks
            _mockUserRepository = new Mock<IUserRepository>();
            _mockLogger = new Mock<ILogger<UserService>>();

            // initialize complex mocks
            _mockUserManager = MockUserManager.Create();
            _mockRoleManager = MockRoleManager.Create();

            // // mock EmailSender (because it's a concrete class, we can mock it directly)
            _mockEmailSender = new Mock<IEmailSender>();



            // initialize the SUT with mocked dependencies
            _userService = new UserService(
                _mockUserRepository.Object,
                _mockUserManager.Object,
                _mockRoleManager.Object,
                _mockEmailSender.Object,
                _mockLogger.Object
            );
        }

        //RegisterUserAsync test

        [Fact]
        public async Task RegisterUserAsync_Should_ReturnSuccess_WhenCreateAsyncSucceeds()
        {
            // Arrange
            var user = new User
            {
                UserName = "testuser",
                Email = "test@example.com",
                FullName = "Test User"
            };
            var password = "StrongPassword123!";

            // Set up successful result from Identity
            var identitySuccessResult = IdentityResult.Success;

            // Setup Mock: when _userManager.CreateAsync is called...
            _mockUserManager
                .Setup(m => m.CreateAsync(user, password))
                // ..return success result
                .ReturnsAsync(identitySuccessResult);

            // Act
            var response = await _userService.RegisterUserAsync(user, password);

            // Assert

            // Check that the returned user is the same as input user
            response.Status.Should().Be((int)HttpStatusCode.OK);
            response.Data.Should().Be(user);
            response.Message.Should().BeNull();
        }

        [Fact]
        public async Task RegisterUserAsync_Should_ReturnFailure_WhenCreateAsyncFails()
        {
            // Arrange
            var user = new User
            {
                UserName = "invaliduser",
                Email = "invalid@example.com"
            };
            var password = "weak"; // ex: too weak password

            // set up failure result from Identity
            var expectedErrors = new[]
            {
                new IdentityError { Code = "PasswordTooShort", Description = "Password is too short." }
            };
            var failureResult = IdentityResult.Failed(expectedErrors);

            // Setup Mock: When _userManager.CreateAsync is called...
            _mockUserManager
                .Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                // ...then return failure result
                .ReturnsAsync(failureResult);

            // Act
            var response = await _userService.RegisterUserAsync(user, password);

            // Assert
            // Check that the status code indicates failure
            response.Status.Should().Be((int)HttpStatusCode.BadRequest);
            response.Data.Should().BeNull();
            response.Message.Should().Contain("User registration failed.");
        }


        //AssignRoleToUserAsync role test
        [Fact]
        public async Task AssignRoleToUserAsync_Should_ReturnSuccess_When_UserAndRoleExistAndUserIsNotInRole()
        {
            // Arrange
            var userId = "existingUserId";
            var roleName = "Admin";
            var user = new User { Id = userId, UserName = "testuser" };

            // User was found
            _mockUserManager.Setup(m => m.FindByIdAsync(userId))
               .ReturnsAsync(user);

            // Role already exist
            _mockRoleManager.Setup(m => m.RoleExistsAsync(roleName))
               .ReturnsAsync(true);

            // User do not have role
            _mockUserManager.Setup(m => m.IsInRoleAsync(user, roleName))
                .ReturnsAsync(false);

            // Add role successfully
            _mockUserManager.Setup(m => m.AddToRoleAsync(user, roleName))
                 .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.AssignRoleToUserAsync(userId, roleName);

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task AssignRoleToUserAsync_Should_ReturnFailed_When_UserNotFound()
        {
            // Arrange
            var userId = "nonExistentUserId";
            var roleName = "Admin";

            // User do not found
            _mockUserManager.Setup(m => m.FindByIdAsync(userId))
            .ReturnsAsync((User)null);

            // Act
            var result = await _userService.AssignRoleToUserAsync(userId, roleName);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Errors.First().Description.Should().Be("User not found.");
        }

        [Fact]
        public async Task AssignRoleToUserAsync_Should_ReturnFailed_When_RoleDoesNotExist()
        {
            // Arrange
            var userId = "existingUserId";
            var roleName = "nonExistentRole";
            var user = new User { Id = userId, UserName = "testuser" };

            // User was founded
            _mockUserManager.Setup(m => m.FindByIdAsync(userId))
                .ReturnsAsync(user);

            // Role does not exist
            _mockRoleManager.Setup(m => m.RoleExistsAsync(roleName))
                .ReturnsAsync(false);

            // Act
            var result = await _userService.AssignRoleToUserAsync(userId, roleName);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Errors.First().Description.Should().Be("Role does not exist.");
        }

        [Fact]
        public async Task AssignRoleToUserAsync_Should_ReturnFailed_WhenUserIsAlreadyInRole()
        {
            // Arrange
            var userId = "existingUserId";
            var roleName = "Admin";
            var user = new User { Id = userId, UserName = "testuser" };

            // 1. User had been find
            _mockUserManager.Setup(m => m.FindByIdAsync(userId))
        .ReturnsAsync(user);

            // 2. Role already exist
            _mockRoleManager.Setup(m => m.RoleExistsAsync(roleName))
        .ReturnsAsync(true);

            // 3. User already had role
            _mockUserManager.Setup(m => m.IsInRoleAsync(user, roleName))
        .ReturnsAsync(true);

            // Act
            var result = await _userService.AssignRoleToUserAsync(userId, roleName);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Errors.First().Description.Should().Be("User also had this role!");
        }

        [Fact]
        public async Task AssignRoleToUserAsync_Should_ReturnFailed_WhenAddToRoleAsyncFails()
        {
            // Arrange
            var userId = "existingUserId";
            var roleName = "Admin";
            var user = new User { Id = userId, UserName = "testuser" };
            var identityError = new IdentityError { Description = "Database error" };
            var failureResult = IdentityResult.Failed(identityError);

            // User had already find
            _mockUserManager.Setup(m => m.FindByIdAsync(userId))
        .ReturnsAsync(user);

            // Role already exist
            _mockRoleManager.Setup(m => m.RoleExistsAsync(roleName))
        .ReturnsAsync(true);

            // User did not have this role
            _mockUserManager.Setup(m => m.IsInRoleAsync(user, roleName))
        .ReturnsAsync(false);

            // 4. Add role fail
            _mockUserManager.Setup(m => m.AddToRoleAsync(user, roleName))
        .ReturnsAsync(failureResult);

            // Act
            var result = await _userService.AssignRoleToUserAsync(userId, roleName);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Errors.First().Description.Should().Be("Database error");
        }

        [Fact]
        public async Task AssignRoleToUserAsync_Should_ReturnFailed_When_ExceptionIsThrown()
        {
            // Arrange
            var userId = "existingUserId";
            var roleName = "Admin";
            var exception = new Exception("Simulated database error");

            // FindByIdAsync throw exception
            _mockUserManager.Setup(m => m.FindByIdAsync(userId))
        .ThrowsAsync(exception);

            // Act
            var result = await _userService.AssignRoleToUserAsync(userId, roleName);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Errors.First().Description.Should().Be("An error occurred while assigning role.");
        }



        [Fact]
        public async Task DeleteAsync_Should_Return204_When_Delete_Succeeds()
        {
            // Arrange
            var userId = "test-user-id";
            var user = new User { Id = userId, UserName = "testuser" };

            _mockUserManager
                .Setup(m => m.FindByIdAsync(userId))
                .ReturnsAsync(user);

            _mockUserManager
                .Setup(m => m.DeleteAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var response = await _userService.DeleteAsync(userId);

            // Assert
            response.Status.Should().Be(ResultStatus.NoContent);
            response.Message.Should().Be("User deleted successfully.");
            response.Data.Should().BeNull();

        }

        [Fact]
        public async Task DeleteAsync_Should_Return500_When_ExceptionThrown()
        {
            // Arrange
            var userId = "test-user-id";

            _mockUserManager
                .Setup(m => m.FindByIdAsync(userId))
                .ThrowsAsync(new Exception("Simulated DB failure"));

            // Act
            var response = await _userService.DeleteAsync(userId);

            // Assert
            response.Status.Should().Be(ResultStatus.InternalError);
            response.Message.Should().Be("An unexpected error occurred during user deletion.");
            response.Data.Should().BeNull();
        }

    }

}
