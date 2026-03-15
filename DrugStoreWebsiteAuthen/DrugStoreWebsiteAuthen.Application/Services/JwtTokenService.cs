using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using DrugStoreWebsiteAuthen.Application.Interfaces;

namespace DrugStoreWebsiteAuthen.Application.Services;

public class JwtService : IJwtService
{
    private readonly IUserService _userService;
    private readonly ILogger<JwtService> _logger;

    public JwtService(IUserService userService, ILogger<JwtService> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public async Task<string> GenerateJwtToken(string username)
    {
        var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET")!;
        if (string.IsNullOrEmpty(secretKey))
        {
            _logger.LogError("JWT_SECRET environment variable is not set.");
            throw new InvalidOperationException("JWT_SECRET not configured");
        }

        try
        {
            var user = await _userService.GetUserByUserNameAsync(username);
            var roles = await _userService.GetUserRolesAsync(username);
            var role = roles.FirstOrDefault() ?? "";

            var key = Encoding.UTF8.GetBytes(secretKey);
            var expirationMinutesString = Environment.GetEnvironmentVariable("JWT_ACCESS_EXPIRATION_MINUTES");
            var expirationMinutes = int.TryParse(expirationMinutesString, out var parsedMinutes) ? parsedMinutes : 5;

            var tokenHandler = new JwtSecurityTokenHandler();
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Sub, user.Data.Id), 

            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
                Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate JWT token for {Username}", username);
            throw;
        }
    }

    public async Task<string> GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
