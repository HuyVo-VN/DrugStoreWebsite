using DrugStoreWebsiteAuthen.Application.DTOs.Request;
using DrugStoreWebsiteAuthen.Application.DTOs.Response;
using DrugStoreWebsiteAuthen.Application.Interfaces;
using DrugStoreWebsiteAuthen.Application.Common;
using DrugStoreWebsiteAuthen.Application.Services;
using DrugStoreWebsiteAuthen.Infrastructure;
using DrugStoreWebsiteAuthen.Infrastructure.Persistence;
using DrugStoreWebsiteAuthen.Infrastructure.Repositories;
using DrugStoreWebsiteAuthen.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using OtpNet;
using QRCoder;

namespace DrugStoreWebsiteAuthen.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        private readonly IJwtService _jwtService;

        public AuthController(IUserService userService, IEmailSender emailSender, IConfiguration configuration, ILogger<AuthController> logger, IJwtService jwtService)
        {
            _userService = userService;
            _emailSender = emailSender;
            _configuration = configuration;
            _logger = logger;
            _jwtService = jwtService;
        }


        // Authenticates a user and returns a JWT token.
        // 200 OK with token, 400 Bad Request on failure, 401 Unauthorized if user doesn't exist, 500 Internal Server Error on unexpected error
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]// Success: Returns token
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseModel<string>))] // Failure: Wrong password or username
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]// Failure: User doesn't exist
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseModel<string>))] // Unexpected server error
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var isValidUser = await _userService.ValidateUserAsync(request.Username, request.Password);
                if (!isValidUser)
                    return BadRequest(Result<string>.Failure(ResultStatus.BadRequest, "Wrong password or username"));

                var userResult = await _userService.GetUserByUserNameAsync(request.Username);
                if (!userResult.Succeeded || userResult.Data == null)
                    return Unauthorized(Result<string>.Failure(ResultStatus.NotFound, "User doesn't exist."));

                var user = userResult.Data;

                if (user.TwoFactorEnabled)
                {
                    _logger.LogInformation("User {Username} requires 2FA verification.", user.UserName);
                    return Ok(new
                    {
                        requires2FA = true,
                        username = user.UserName
                    });
                }

                var accessToken = await _jwtService.GenerateJwtToken(user.UserName);
                var refreshToken = await _jwtService.GenerateRefreshToken();

                var expirationDaysString = Environment.GetEnvironmentVariable("JWT_REFRESH_EXPIRATION_DAYS");
                var refreshTokenExpiryDays = int.TryParse(expirationDaysString, out var parsedMinutes) ? parsedMinutes : 15;

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshTokenExpiryDays);
                await _userService.UpdateUserAsync(user);

                return Ok(new
                {
                    token = accessToken,
                    refreshToken = refreshToken
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in login process for user {Username}.", request.Username);
                return BadRequest(Result<string>.Failure(ResultStatus.NotFound, ex.Message));
            }
        }

        [HttpPost("google-login")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseModel<string>))]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequestDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.IdToken))
                    return BadRequest(Result<string>.Failure(ResultStatus.BadRequest, "Tokens cannot be empty."));

                // Lấy ClientId từ appsettings.json hoặc .env
                var clientId = Environment.GetEnvironmentVariable("Google__ClientId") ?? _configuration["Google:ClientId"];

                if (string.IsNullOrEmpty(clientId))
                {
                    _logger.LogError("SERIOUS ERROR: Google ClientId not found in .env or appsettings!");
                    return StatusCode(500, Result<string>.Failure(ResultStatus.InternalError, "Google ClientID has not been configured in the backend."));
                }
                if (string.IsNullOrEmpty(clientId))
                    return StatusCode(500, Result<string>.Failure(ResultStatus.InternalError, "Google ClientID has not been configured in the backend."));

                // 1. Xác thực với Google qua UserService
                var userResult = await _userService.GoogleLoginAsync(request.IdToken, clientId);
                if (!userResult.Succeeded || userResult.Data == null)
                    return BadRequest(Result<string>.Failure(ResultStatus.BadRequest, userResult.Message));

                var user = userResult.Data;

                if (user.TwoFactorEnabled)
                {
                    _logger.LogInformation("User {Username} logged in via Google but requires 2FA.", user.UserName);
                    return Ok(new
                    {
                        requires2FA = true,
                        username = user.UserName
                    });
                }


                // 2. Sinh JWT Token và Refresh Token giống hệt logic Login thường
                var accessToken = await _jwtService.GenerateJwtToken(user.UserName);
                var refreshToken = await _jwtService.GenerateRefreshToken();

                var expirationDaysString = Environment.GetEnvironmentVariable("JWT_REFRESH_EXPIRATION_DAYS");
                var refreshTokenExpiryDays = int.TryParse(expirationDaysString, out var parsedMinutes) ? parsedMinutes : 15;

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshTokenExpiryDays);
                await _userService.UpdateUserAsync(user);

                // 3. Trả về Token cho Frontend
                return Ok(new
                {
                    token = accessToken,
                    refreshToken = refreshToken
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Google Login Controller.");
                return BadRequest(Result<string>.Failure(ResultStatus.InternalError, ex.Message));
            }
        }

        [HttpPost("reset-password"), AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))] // Success
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseModel<string>))] // Identity errors
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseModel<string>))] // Decoding or server failure
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var response = new ResponseModel<string>();

            try
            {
                var decodedTokenBytes = WebEncoders.Base64UrlDecode(request.Token);
                var decodedToken = System.Text.Encoding.UTF8.GetString(decodedTokenBytes);

                var result = await _userService.ResetPasswordAsync(request.Email, decodedToken, request.NewPassword);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        _logger.LogError("Password reset failed for email {Email}. Error: {ErrorDescription}", request.Email, error.Description);
                        response.Status = 400;
                        response.Message = error.Description;
                    }
                    return StatusCode(400, response);
                }

                _logger.LogInformation("Password successfully reset for email: {Email}.", request.Email);
                return Ok("Password has been reset.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ResetPassword Error for email: {Email}. Decoding or server failure.", request.Email);
                response.Status = 500;
                response.Message = "Change password failed";
                return StatusCode(500, response);
            }
        }
        [HttpPost("forget-password"), AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseModel<string>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseModel<string>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ResponseModel<string>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseModel<string>))]
        public async Task<IActionResult> ForgetPassword([FromBody] ForgetPasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid forget-password request for email: {Email}", request.Email);
                    return BadRequest(new ResponseModel<string>
                    {
                        Status = 400,
                        Message = "Invalid request data.",
                        Data = null
                    });
                }

                var baseUrl = _configuration["FrontendUrl"];
                var response = await _userService.SendPasswordResetLinkAsync(request.Email, baseUrl);
                if (response.Status == 404)
                {
                    return NotFound(response);
                }
                else if (response.Status != 200)
                {
                    return StatusCode(response.Status, response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ForgetPassword error for email: {Email}", request.Email);
                return StatusCode(500, new ResponseModel<string>
                {
                    Status = 500,
                    Message = "Unexpected error occurred.",
                    Data = null
                });
            }
        }


        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ResponseModel<string>))] // Success
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseModel<string>))] // Model state invalid or user exists
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseModel<string>))] // Internal server error
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var response = new ResponseModel<string>();

            if (!ModelState.IsValid)
            {
                _logger.LogError("Register request failed model validation for user: {UserName}.", request.UserName);
                response.Status = 400;
                response.Message = "Invalid request data";
                response.Data = null;
                return BadRequest(ModelState);
            }

            try
            {
                var user = new User
                {
                    UserName = request.UserName,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    FullName = request.FullName,
                    Gender = request.Gender,
                    ImageUrl = request.ImageUrl
                };

                var result = await _userService.RegisterUserAsync(user, request.Password);

                if (!result.Status.Equals(200))
                {
                    _logger.LogWarning("Registration failed for user: {UserName}. Reasons: {Errors}", request.UserName, result.Message);
                    response.Status = 400;
                    response.Message = "Registration failed.";
                    // Return all error descriptions from IdentityResult
                    response.Data = result.Message;
                    return BadRequest(response);
                }

                _logger.LogInformation("User {UserName} registered successfully.", request.UserName);
                response.Status = 201; // Use 201 Created
                response.Message = "User registered successfully.";
                response.Data = "User ID: " + result.Data?.Id;
                return CreatedAtAction(nameof(Register), response); // Return 201 Created
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during registration for user: {UserName}.", request.UserName);
                response.Status = 500;
                response.Message = "An internal server error occurred.";
                response.Data = ex.Message;
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("GetPrincipalFromExpiredToken: Token is null or empty.");
                return null;
            }
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                    Environment.GetEnvironmentVariable("JWT_SECRET")!
                )),
                ValidateIssuer = true,
                ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
                ValidateAudience = true,
                ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
                ValidateLifetime = false,
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                SecurityToken securityToken;

                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);

                var jwtSecurityToken = securityToken as JwtSecurityToken;
                if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.LogError("Invalid token algorithm.");
                }

                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token in GetPrincipalFromExpiredToken.");
                return null;
            }
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseModel<object>))] // Success
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseModel<string>))] // Invalid token or request
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseModel<string>))] // Internal error
        public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto refreshDto)
        {
            var response = new ResponseModel<object>();

            if (refreshDto == null || string.IsNullOrEmpty(refreshDto.AccessToken) || string.IsNullOrEmpty(refreshDto.RefreshToken))
            {
                _logger.LogWarning("Refresh token request failed due to missing tokens.");
                response.Status = 400;
                response.Message = "Invalid client request: tokens are required.";
                return BadRequest(response);
            }

            try
            {
                var principal = GetPrincipalFromExpiredToken(refreshDto.AccessToken);
                if (principal == null || principal?.Identity?.Name is null)
                {
                    _logger.LogWarning("Invalid access token provided for refresh.");
                    response.Status = 400;
                    response.Message = "Invalid access token.";
                    return BadRequest(response);
                }

                var username = principal.Identity.Name;
                var user = await _userService.GetUserByUserNameAsync(username);

                if (user == null || user.Data.RefreshToken != refreshDto.RefreshToken || user.Data.RefreshTokenExpiryTime <= DateTime.UtcNow)
                {
                    _logger.LogWarning("Invalid refresh token for user: {UserName}.", username);
                    response.Status = 400;
                    response.Message = "Invalid refresh token or token expired.";
                    return BadRequest(response);
                }

                var newAccessToken = await _jwtService.GenerateJwtToken(user.Data.UserName);

                var newRefreshToken = await _jwtService.GenerateRefreshToken();

                var expirationDaysString = Environment.GetEnvironmentVariable("JWT_REFRESH_EXPIRATION_DAYS");
                var refreshTokenExpiryDays = int.TryParse(expirationDaysString, out var parsedMinutes) ? parsedMinutes : 15;

                await _userService.SetRefreshTokenAsync(user.Data.UserName, newRefreshToken, refreshTokenExpiryDays);

                _logger.LogInformation("Token refreshed successfully for user: {UserName}.", username);
                response.Status = 200;
                response.Message = "Tokens refreshed successfully.";
                response.Data = new { AccessToken = newAccessToken, RefreshToken = newRefreshToken };
                return Ok(response);
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogError(ex, "Security token validation failed during refresh.");
                response.Status = 400;
                response.Message = "Invalid access token.";
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during token refresh.");
                response.Status = 500;
                response.Message = "An internal server error occurred.";
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        [Authorize]
        [HttpPost("revoke")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseModel<string>))] // Success
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponseModel<string>))] // User not found from token
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ResponseModel<string>))] // Internal error
        public async Task<IActionResult> Revoke()
        {
            var response = new ResponseModel<string>();

            try
            {
                var username = User.Identity?.Name;
                if (username == null)
                {
                    _logger.LogWarning("Revoke failed: could not determine user from token.");
                    response.Status = 400;
                    response.Message = "Invalid token: cannot identify user.";
                    return BadRequest(response);
                }

                await _userService.RevokeRefreshTokenAsync(username);

                _logger.LogInformation("Refresh token revoked for user: {UserName}. User logged out.", username);
                response.Status = 200;
                response.Message = "Success";
                response.Data = "Refresh token revoked. User has been logged out.";
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while revoking token for user: {UserName}.", User.Identity?.Name);
                response.Status = 500;
                response.Message = "An internal server error occurred.";
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        [Authorize]
        [HttpGet("setup-2fa")]
        public async Task<IActionResult> Setup2FA()
        {
            var username = User.Identity?.Name;
            var userResult = await _userService.GetUserByUserNameAsync(username);
            if (!userResult.Succeeded) return BadRequest("User not found");

            var user = userResult.Data;

            // 1. Sinh ra một Secret Key ngẫu nhiên (16 byte)
            var key = KeyGeneration.GenerateRandomKey(20);
            var secretString = Base32Encoding.ToString(key);

            // 2. Tạm thời lưu Secret này vào user (chưa kích hoạt vội)
            user.TwoFactorSecret = secretString;
            await _userService.UpdateUserAsync(user);

            // 3. Tạo đường dẫn theo chuẩn của Google Authenticator
            var issuer = "DrugStoreHuyVo"; // Tên App sẽ hiện trong điện thoại
            var uriString = $"otpauth://totp/{issuer}:{user.Email}?secret={secretString}&issuer={issuer}";

            // 4. Dùng QRCoder biến đường dẫn đó thành hình ảnh QR (Base64)
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(uriString, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(20);

            var base64QrCode = Convert.ToBase64String(qrCodeImage);

            // 5. Trả về cho Frontend
            return Ok(new
            {
                QrCodeImage = $"data:image/png;base64,{base64QrCode}",
                ManualSetupKey = secretString
            });
        }

        [Authorize]
        [HttpPost("verify-2fa-setup")]
        public async Task<IActionResult> Verify2FASetup([FromBody] string code)
        {
            var username = User.Identity?.Name;
            var userResult = await _userService.GetUserByUserNameAsync(username);
            if (!userResult.Succeeded) return BadRequest("User not found");

            var user = userResult.Data;

            if (string.IsNullOrEmpty(user.TwoFactorSecret))
                return BadRequest("2FA is not initialized. Please call setup first.");

            // 1. Lấy Secret Key trong DB ra và nạp vào máy tính toán
            var secretKeyBytes = Base32Encoding.ToBytes(user.TwoFactorSecret);
            var totp = new Totp(secretKeyBytes);

            // 2. Kiểm tra xem mã 6 số user gửi lên có khớp với thời gian hiện tại không
            // (Cho phép du di 1 khoảng thời gian 30s đề phòng đồng hồ lệch)
            bool isCorrect = totp.VerifyTotp(code, out long timeWindowUsed, VerificationWindow.RfcSpecifiedNetworkDelay);

            if (!isCorrect)
            {
                return BadRequest(new { status = 400, message = "Invalid 2FA code. Please try again." });
            }

            // 3. Đúng mã -> Bật cờ kích hoạt chính thức
            user.TwoFactorEnabled = true;
            await _userService.UpdateUserAsync(user);

            return Ok(new { status = 200, message = "2FA enabled successfully!" });
        }

        [HttpPost("login-2fa")]
        [AllowAnonymous] // Phải để AllowAnonymous vì lúc này user CHƯA CÓ Token
        public async Task<IActionResult> Login2FA([FromBody] Login2FARequest request)
        {
            var userResult = await _userService.GetUserByUserNameAsync(request.Username);
            if (!userResult.Succeeded || userResult.Data == null)
                return BadRequest("Tài khoản không tồn tại");

            var user = userResult.Data;

            // Kiểm tra OTP
            var secretKeyBytes = Base32Encoding.ToBytes(user.TwoFactorSecret);
            var totp = new Totp(secretKeyBytes);
            bool isCorrect = totp.VerifyTotp(request.Code, out _);

            if (!isCorrect) return BadRequest(Result<string>.Failure(ResultStatus.BadRequest, "Mã xác thực không đúng"));

            // Nếu đúng -> Sinh Token y hệt như hàm Login thường
            var accessToken = await _jwtService.GenerateJwtToken(user.UserName);
            var refreshToken = await _jwtService.GenerateRefreshToken();

            var expirationDaysString = Environment.GetEnvironmentVariable("JWT_REFRESH_EXPIRATION_DAYS");
            var refreshTokenExpiryDays = int.TryParse(expirationDaysString, out var parsedMinutes) ? parsedMinutes : 15;

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshTokenExpiryDays);
            await _userService.UpdateUserAsync(user);

            return Ok(new
            {
                token = accessToken,
                refreshToken = refreshToken
            });
        }

        [Authorize] // Bắt buộc phải có Token mới được đổi
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                // 1. Lấy tên user từ Token đang đăng nhập
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized(new { message = "Không xác định được người dùng." });

                var userResult = await _userService.GetUserByUserNameAsync(username);
                if (!userResult.Succeeded || userResult.Data == null)
                    return BadRequest(new { message = "User not found." });

                var user = userResult.Data;

                // 2. Kiểm tra mật khẩu cũ có đúng không
                var isOldPasswordValid = await _userService.ValidateUserAsync(username, request.OldPassword);
                if (!isOldPasswordValid)
                {
                    return BadRequest(new { message = "Mật khẩu cũ không chính xác." });
                }

                // 3. Thực hiện đổi sang mật khẩu mới (Sếp cần gọi hàm đổi pass của UserManager hoặc UserService ở đây)
                // Ví dụ: var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
                // Giả định sếp có hàm ChangePasswordAsync trong IUserService:
                var result = await _userService.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);

                if (!result.Succeeded)
                {
                    return BadRequest(new { message = "Không thể đổi mật khẩu. " + string.Join(", ", result.Errors.Select(e => e.Description)) });
                }

                return Ok(new { status = 200, message = "Đổi mật khẩu thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đổi mật khẩu cho user.");
                return StatusCode(500, new { message = "Lỗi hệ thống khi đổi mật khẩu." });
            }
        }

        [Authorize]
        [HttpPost("disable-2fa")]
        public async Task<IActionResult> Disable2FA()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var userResult = await _userService.GetUserByUserNameAsync(username);
            if (!userResult.Succeeded || userResult.Data == null) return BadRequest("User not found");

            var user = userResult.Data;

            // Tắt cờ và xóa chìa khóa bí mật đi
            user.TwoFactorEnabled = false;
            user.TwoFactorSecret = null;

            await _userService.UpdateUserAsync(user);

            return Ok(new { status = 200, message = "2FA disabled successfully!" });
        }
    }

}