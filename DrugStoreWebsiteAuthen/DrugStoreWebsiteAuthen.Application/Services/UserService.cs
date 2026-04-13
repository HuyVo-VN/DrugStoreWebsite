using DrugStoreWebsiteAuthen.Domain;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using System.Security.Principal;
using DrugStoreWebsiteAuthen.Application.DTOs;
using DrugStoreWebsiteAuthen.Application.Common;
using DrugStoreWebsiteAuthen.Application.Interfaces;
using System.Text;
using Azure;
using Google.Apis.Auth;

namespace DrugStoreWebsiteAuthen.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<UserService> _logger;


    public UserService(IUserRepository userRepository, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IEmailSender emailSender, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _userManager = userManager;
        _roleManager = roleManager;
        _emailSender = emailSender;
        _logger = logger;
    }
    public async Task<IEnumerable<User>> GetUsers()
    {
        try
        {
            return await _userRepository.GetAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in GetUsers: {ex.Message}");
            return Enumerable.Empty<User>();
        }
    }

    public async Task<IEnumerable<object>> GetUsersWithRole()
    {
        var usersWithRoles = new List<object>();

        try
        {
            var users = await _userRepository.GetAllAsync();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                usersWithRoles.Add(new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                    user.FullName,
                    user.Gender,
                    user.DateOfBirth,
                    user.PhoneNumber,
                    Roles = roles
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetUsersWithRole");
        }

        return usersWithRoles;
    }

    public async Task<bool> ValidateUserAsync(string userName, string password)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
                return false;

            return await _userManager.CheckPasswordAsync(user, password);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in ValidateUserAsync: {ex.Message}");
            return false;
        }
    }

    public async Task<Result<User>> GetUserByUserNameAsync(string userName)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                _logger.LogWarning($"User {userName} not found.");
                return Result<User>.Failure(ResultStatus.NotFound, "User not found.");
            }
            return Result<User>.Success(ResultStatus.Success, user, "User found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetUserByUserNameAsync {userName}", userName);
            return Result<User>.Failure(ResultStatus.InternalError, "An unexpected error occurred during get user by user name.");
        }
    }

    public async Task SeedAdminUserAsync(string adminUserName, string adminPassword)
    {
        try
        {
            var roleAdminExists = await _roleManager.RoleExistsAsync("Admin");
            var roleCustomerExists = await _roleManager.RoleExistsAsync("Customer");
            var roleStaffExists = await _roleManager.RoleExistsAsync("Staff");

            if (!roleAdminExists)
            {
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
                _logger.LogInformation("Role 'Admin' created successfully.");

            }
            if (!roleCustomerExists)
            {
                await _roleManager.CreateAsync(new IdentityRole("Customer"));
                _logger.LogInformation("Role 'Customer' created successfully.");
            }

            if (!roleStaffExists)
            {
                await _roleManager.CreateAsync(new IdentityRole("Staff"));
                _logger.LogInformation("Role 'Staff' created successfully.");

            }
            var user = await _userManager.FindByNameAsync(adminUserName);
            if (user == null)
            {
                var adminUser = new User
                {
                    UserName = adminUserName,
                    Email = "Admin@gmail.com",
                    PhoneNumber = "0123456789",
                    FullName = "Admin User"
                };

                var result = await _userManager.CreateAsync(adminUser, adminPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to create admin user: {Errors}", errors);
                }
                _logger.LogInformation($"Admin user {adminUserName} created successfully.");

                user = adminUser;
            }
            else
            {
                _logger.LogInformation("Admin user already exists");
            }

            var hasRole = await _userManager.IsInRoleAsync(user, "Admin");

            if (!hasRole)
            {
                var addRoleResult = await _userManager.AddToRoleAsync(user, "Admin");

                if (addRoleResult.Succeeded)
                {
                    _logger.LogInformation($"Assigned 'Admin' role to user {adminUserName}");
                }
                else
                {
                    var errors = string.Join(", ", addRoleResult.Errors.Select(e => e.Description));
                    _logger.LogError($"Failed to assign 'Admin' role to user {user.UserName}. Errors: {errors}");
                }
            }
            else
            {
                _logger.LogInformation("Admin user already has 'Admin' role.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in SeedAdminUserAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<IList<string>> GetUserRolesAsync(string userName)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                _logger.LogWarning($"Attempt to get roles for non-existent user: {userName}");
                return new List<string>();
            }
            return await _userManager.GetRolesAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error occurred while retrieving roles for user {userName}.");
            return new List<string>();
        }
    }

    // Update user method to be reused
    public async Task UpdateUserAsync(User user)
    {
        try
        {
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to update user: {Errors}", errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in UpdateUserAsync: {ex.Message}");
            throw;
        }
    }

    public async Task SetRefreshTokenAsync(string userName, string refreshToken, double daysUntilExpiry)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                _logger.LogError("Set token: User {Username} not found.", userName);
            }

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(daysUntilExpiry);

            await UpdateUserAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in SetRefreshTokenAsync: {ex.Message}");
            throw;
        }
    }

    public async Task RevokeRefreshTokenAsync(string userName)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                // Silently ignore if user not found
                _logger.LogInformation("Revoke token: User {Username} not found. Silently ignoring.", userName);
                return;
            }

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = DateTime.UtcNow; // Expire immediately

            await UpdateUserAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in RevokeRefreshTokenAsync: {ex.Message}");
            throw;
        }
    }


    public async Task<bool> UserHasRoleAsync(string userName, string roleName)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
                return false;

            var roles = await _userManager.GetRolesAsync(user);
            return roles.Contains(roleName);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in UserHasRoleAsync: {ex.Message}");
            return false;
        }
    }

    public async Task<ResponseModel<string>> GenerateResetPasswordTokenAsync(string email)
    {
        var result = new ResponseModel<string>();
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("GenerateResetPasswordTokenAsync called with null or empty email.");
            result.Status = 400;
            result.Message = "Email is invalid";
            return result;
        }
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning($"Attempt to generate reset token for non-existent email: {email}");
                result.Status = 404;
                result.Message = "Email not found.";
                return result;
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            _logger.LogDebug($"Generated password reset token for user {user.Email}.");

            result.Data = token;
            result.Status = 200;
            result.Message = "Generated reset token successfully";
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to generate password reset token for email: {email}");
            result.Status = 500;
            result.Message = "Internal server error.";
            return result;
        }
    }

    public async Task<IdentityResult> ResetPasswordAsync(string email, string token, string newPassword)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning($"Attempted password reset for invalid email: {email}");
                return IdentityResult.Failed(new IdentityError { Description = "Invalid email." });
            }

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning($"Password reset failed for user {email}. Errors: {errors}");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"System error during password reset for user: {email}");
            return IdentityResult.Failed(new IdentityError { Description = "An unexpected error occurred during password reset." });
        }
    }

    public async Task<ResponseModel<string>> SendPasswordResetLinkAsync(string email, string baseUrl)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning($"Password reset requested for non-existent email: {email}");
                return new ResponseModel<string>
                {
                    Status = 404,
                    Message = "User not found.",
                };
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var resetLink = $"{baseUrl}/reset-password?email={Uri.EscapeDataString(email)}&token={encodedToken}";

            _logger.LogInformation($"Sending password reset link for email: {email}");

            return new ResponseModel<string>
            {
                Status = 200,
                Message = "Password reset link created successfully.",
                Data = resetLink
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating reset link.");
            return new ResponseModel<string>
            {
                Status = 500,
                Message = "Internal server error.",
            };
        }
    }

    public async Task<ResponseModel<User>> RegisterUserAsync(User user, string password)
    {
        var result = new ResponseModel<User>();
        var resultIdentityUser = await _userManager.CreateAsync(user, password);
        if (!resultIdentityUser.Succeeded)
        {
            var errors = string.Join(", ", resultIdentityUser.Errors.Select(e => e.Description));
            _logger.LogError("Failed to register user: {Errors}", errors);
            result.Status = 400;
            result.Message = "User registration failed.";
            return result;
        }

        // Assign default role "Customer" to the newly registered user
        var addToRoleResult = await _userManager.AddToRoleAsync(user, "Customer");
        if (!addToRoleResult.Succeeded)
        {
            var errors = string.Join(", ", addToRoleResult.Errors.Select(e => e.Description));
            _logger.LogError("Failed to assign role 'Customer' to user: {UserName}, Errors: {Errors}", user.UserName, errors);
            result.Status = 400;
            result.Message = "Failed to assign default role 'Customer'.";
            return result;
        }

        _logger.LogInformation("User {UserName} registered successfully", user.UserName);
        result.Status = 200;
        result.Data = user;
        return result;
    }

    public async Task<Result<string>> DeleteAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"User {userId} not found.");
                return Result<string>.Failure(ResultStatus.NotFound, "User not found.");
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToArray();
                return Result<string>.Failure(ResultStatus.BadRequest, errors);
            }

            return Result<string>.Success(ResultStatus.NoContent, "User deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return Result<string>.Failure(ResultStatus.InternalError, "An unexpected error occurred during user deletion.");
        }
    }

    public async Task<IdentityResult> AssignRoleToUserAsync(string userId, string roleName)
    {
        try
        {
            // Check if user exists
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError("AssignRoleToUserAsync: User with ID {UserId} not found.", userId);
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            }

            // Check if role exists
            var roleExists = await _roleManager.RoleExistsAsync(roleName);
            if (!roleExists)
            {
                _logger.LogError("AssignRoleToUserAsync: Role {RoleName} does not exist.", roleName);
                return IdentityResult.Failed(new IdentityError { Description = "Role does not exist." });
            }

            // Check if user already has the role
            var isAlreadyInRole = await _userManager.IsInRoleAsync(user, roleName);
            if (isAlreadyInRole)
            {
                _logger.LogWarning("AssignRoleAsync: User {UserName} also had {RoleName}", user.UserName, roleName);
                return IdentityResult.Failed(new IdentityError { Description = "User also had this role!" });
            }

            //Remove old roles (if any)
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to remove old roles: {Errors}", errors);
                    return IdentityResult.Failed(new IdentityError { Description = "Failed to remove old roles." });
                }

                _logger.LogInformation("Removed old roles from user {UserId}: {Roles}", userId, string.Join(", ", currentRoles));
            }

            // Assign role to user
            var addRoleresult = await _userManager.AddToRoleAsync(user, roleName);
            if (!addRoleresult.Succeeded)
            {
                var errors = string.Join(", ", addRoleresult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to assign role: {Errors}", errors);
            }
            else
            {
                _logger.LogInformation("Assigned role {RoleName} to user {UserId}", roleName, userId);
            }

            return addRoleresult;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in AssignRoleToUserAsync: {ex.Message}");
            return IdentityResult.Failed(new IdentityError { Description = "An error occurred while assigning role." });
        }
    }

    public async Task<Result<User>> GoogleLoginAsync(string idToken, string clientId)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new List<string>() { clientId }
            };

            // 1. Google xác minh Token
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            // 2. Kiểm tra xem Email đã tồn tại chưa
            var user = await _userManager.FindByEmailAsync(payload.Email);

            if (user == null)
            {
                // Tự động tạo tài khoản mới nếu lần đầu đăng nhập bằng Google
                // Thêm random Guid vào Username để chống trùng lặp nếu có người dùng email khác nhưng trùng tên
                user = new User
                {
                    UserName = payload.Email.Split('@')[0] + "_" + Guid.NewGuid().ToString().Substring(0, 4),
                    Email = payload.Email,
                    FullName = payload.Name,
                    EmailConfirmed = true,
                    // ImageUrl = payload.Picture // Mở comment nếu Entity User của sếp có thuộc tính này
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    return Result<User>.Failure(ResultStatus.InternalError, "Unable to create an account from Google.");
                }

                // Cấp quyền Customer mặc định
                await _userManager.AddToRoleAsync(user, "Customer");
            }

            // Trả về User xịn để Controller sinh Token
            return Result<User>.Success(ResultStatus.Success, user, "Google verification successful");
        }
        catch (InvalidJwtException)
        {
            _logger.LogWarning("The Google Token is invalid or has expired.");
            return Result<User>.Failure(ResultStatus.BadRequest, "The Google Token is invalid or has expired!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "System error when logging in with Google.");
            return Result<User>.Failure(ResultStatus.InternalError, "A system error occurred while logging in.");
        }
    }


}


