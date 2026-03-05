using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace DrugStoreWebsiteAuthen.Api.Helpers;

public class JwtSecurityTokenHandlerWrapper
{
    // Create an instance of JwtSecurityTokenHandler
    private readonly JwtSecurityTokenHandler _jwtSecurityTokenHandler;

    public JwtSecurityTokenHandlerWrapper()
    {
        _jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
    }

    // // Generate a JWT token based on user ID and role
    // public string GenerateJwtToken(string userId, string role)
    // {
    //     // Retrieve the JWT secret from environment variables and encode it
    //     var key = Encoding.ASCII.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET")!);

    //     // Create claims for user identity and role
    //     var claims = new List<Claim>
    //         {
    //             new Claim(ClaimTypes.NameIdentifier, userId),
    //             new Claim(ClaimTypes.Role, role)
    //         };

    //     // Create an identity from the claims
    //     var identity = new ClaimsIdentity(claims);

    //     // Describe the token settings
    //     var tokenDescriptor = new SecurityTokenDescriptor
    //     {
    //         Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
    //         Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
    //         Subject = identity,
    //         Expires = DateTime.UtcNow.AddMinutes(30),
    //         SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    //     };

    //     // Create a JWT security token
    //     var token = _jwtSecurityTokenHandler.CreateJwtSecurityToken(tokenDescriptor);

    //     // Write the token as a string and return it
    //     return _jwtSecurityTokenHandler.WriteToken(token);
    // }

    // Validate a JWT token
    public ClaimsPrincipal ValidateJwtToken(string token)
    {
        // Retrieve the JWT secret from environment variables and encode it
        var key = Encoding.ASCII.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET")!);

        try
        {
            // Create a token handler and validate the token
            var tokenHandler = new JwtSecurityTokenHandler();
            var claimsPrincipal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
                ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
                IssuerSigningKey = new SymmetricSecurityKey(key)
            }, out SecurityToken validatedToken);

            // Return the claims principal
            return claimsPrincipal;
        }
        catch (SecurityTokenExpiredException)
        {
            // Handle token expiration
            throw new ApplicationException("Token has expired.");
        }
    }
}
