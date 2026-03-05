using System;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using DrugStoreWebsiteAuthen.Api.Helpers;
using Microsoft.AspNetCore.Http;

namespace DrugStoreWebsiteAuthen.Api.Middlewares;

public class JwtMiddleware: IMiddleware
{
    private readonly JwtSecurityTokenHandlerWrapper _jwtSecurityTokenHandler;

    public JwtMiddleware(JwtSecurityTokenHandlerWrapper jwtSecurityTokenHandler){
        _jwtSecurityTokenHandler = jwtSecurityTokenHandler;
    }
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Get the token from the Authorization header
        var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    // Validate the token 
                    var claimsPrincipal = _jwtSecurityTokenHandler.ValidateJwtToken(token);
                    
                    // Get user ID from claims
                    var userId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    
                    // Save in HttpContext.Items
                    context.Items["UserId"] = userId;
                }
                catch (Exception)
                {
                    // error: Token invalid (ex: expired, tampered, etc.)
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Unauthorized");
                    return;
                }
            }

            // if no token, proceed without setting user context
            await next(context);
    }

}
