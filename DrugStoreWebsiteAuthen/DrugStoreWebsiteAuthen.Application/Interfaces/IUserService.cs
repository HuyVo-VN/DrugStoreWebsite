using DrugStoreWebsiteAuthen.Application.Common;
using DrugStoreWebsiteAuthen.Application.DTOs;
using DrugStoreWebsiteAuthen.Domain;
using Microsoft.AspNetCore.Identity;

namespace DrugStoreWebsiteAuthen.Application.Interfaces;

// The Abstraction used by the API/Controller layer
public interface IUserService
{
    Task<IEnumerable<User>> GetUsers();
    Task UpdateUserAsync(User user);
    Task<bool> ValidateUserAsync(string userName, string password);
    Task<Result<User>> GetUserByUserNameAsync(string userName);
    Task SeedAdminUserAsync(string adminUserName, string adminPassword);
    Task<bool> UserHasRoleAsync(string userName, string roleName);
    Task<IList<string>> GetUserRolesAsync(string userName);
    Task<IEnumerable<object>> GetUsersWithRole();

    Task<ResponseModel<string>> GenerateResetPasswordTokenAsync(string email);
    Task<IdentityResult> ResetPasswordAsync(string email, string token, string newPassword);
    Task<ResponseModel<string>> SendPasswordResetLinkAsync(string email, string baseUrl);
    Task SetRefreshTokenAsync(string userName, string refreshToken, double daysUntilExpiry);
    Task RevokeRefreshTokenAsync(string userName);
    Task<ResponseModel<User>> RegisterUserAsync(User user, string password);
    Task<Result<string>> DeleteAsync(string userId);
    Task<IdentityResult> AssignRoleToUserAsync(string userId, string roleName);
    Task<Result<User>> GoogleLoginAsync(string idToken, string clientId);
    Task<IdentityResult> ChangePasswordAsync(User user, string currentPassword, string newPassword);
}