using DrugStoreWebsiteAuthen.Application;
using DrugStoreWebsiteAuthen.Infrastructure;
using DrugStoreWebsiteAuthen.Infrastructure.Persistence;
using DrugStoreWebsiteAuthen.Infrastructure.Repositories;
using DrugStoreWebsiteAuthen.Domain;
using DrugStoreWebsiteAuthen.Application.DTOs.Request;
using DrugStoreWebsiteAuthen.Application.DTOs.Response;
using DrugStoreWebsiteAuthen.Application.Interfaces;
using DrugStoreWebsiteAuthen.Application.Common;
using DrugStoreWebsiteAuthen.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Google.Apis.Gmail.v1;

namespace DrugStoreWebsiteAuthen.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;

        public UserController(IUserService userService, ILogger<AuthController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet("get-users")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseModel<List<User>>))] // Success
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseModel<string>))] // Not found users
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseModel<string>))] // Internal server error                                                                                                  
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _userService.GetUsersWithRole();
                if (users == null || !users.Any())
                {
                    _logger.LogWarning("No users found.");
                    var notFoundResponse = new ResponseModel<string>
                    {
                        Status = 404,
                        Message = "No users found.",
                        Data = null
                    };
                    return NotFound(notFoundResponse);
                }

                return Ok(new ResponseModel<object>
                {
                    Status = 200,
                    Message = "Success",
                    Data = users
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting users");
                var errorResponse = new ResponseModel<string>
                {
                    Status = 500,
                    Message = "Error while getting users",
                    Data = null
                };
                return StatusCode(500, errorResponse);
            }
        }

        [HttpGet("get-user-by-username")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseModel<List<User>>))] // Success
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseModel<string>))] // Not found users
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseModel<string>))] // Internal server error                                                                                                  
        public async Task<IActionResult> GetUserByUserName(string userName)
        {
            try
            {
                var result = await _userService.GetUserByUserNameAsync(userName);
                if (!result.Succeeded)
                 if (!result.Succeeded)
                    return NotFound(result); 

                return Ok(result);
            }
            catch (Exception ex)
            { 
                _logger.LogError(ex, "ERROR - GetUserByUserName - An unexpected error occurred during get user");
                return BadRequest(ex);
            }
        }

        [HttpDelete("delete")]
        [Authorize(Roles = RoleNames.Admin)]
        [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(Result<string>))] // Delete success
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Result<string>))] // User doesn't exists
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(Result<string>))] // Internal server error

        public async Task<IActionResult> Delete([FromBody] DeleteUserDto deleteUserDto)
        {
            try
            {
                var result = await _userService.DeleteAsync(deleteUserDto.UserId);

                if (!result.Succeeded)
                    return StatusCode((int)result.Status, result); //404 or 400

                return Ok(result);
            }
            catch (Exception ex)
            { 
                _logger.LogError(ex, "ERROR - Delete - An unexpected error occurred during deletion for user");
                return BadRequest(ex);
            }
        }

        [HttpPost("assign-role")]
        [Authorize(Roles = RoleNames.Admin)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseModel<string>))] // Success
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseModel<string>))] // Invalid request or user/role not found
        [ProducesResponseType(StatusCodes.Status401Unauthorized)] // Not have token
        [ProducesResponseType(StatusCodes.Status403Forbidden)] // Have token but not Admin role
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleDto assignRoleDto)
        {
            var response = new ResponseModel<string>();

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("AssignRole request failed model validation.");
                response.Status = 400;
                response.Message = "Invalid request data";
                response.Data = null;
                return BadRequest(response);
            }

            try
            {
                // Call the service to assign role
                var result = await _userService.AssignRoleToUserAsync(assignRoleDto.UserId, assignRoleDto.RoleName);

                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("AssignRole failed for UserId: {UserId}, RoleName: {RoleName}. Reasons: {Errors}",
                    assignRoleDto.UserId, assignRoleDto.RoleName, errors);
                    response.Status = 400;
                    response.Message = "Assign role failed.";
                    response.Data = errors;
                    return BadRequest(response);
                }

                _logger.LogInformation("Role {RoleName} assigned to user {UserId} successfully.",
                assignRoleDto.RoleName, assignRoleDto.UserId);
                response.Status = 200;
                response.Message = "Role assigned successfully.";
                response.Data = null;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during role assignment for UserId: {UserId}.",
                assignRoleDto.UserId);
                response.Status = 500;
                response.Message = "An internal server error occurred.";
                response.Data = ex.Message;
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }


    }

}
