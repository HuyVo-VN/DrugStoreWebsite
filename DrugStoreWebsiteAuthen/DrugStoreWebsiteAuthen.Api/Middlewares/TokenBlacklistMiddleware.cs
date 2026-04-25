using Microsoft.Extensions.Caching.Distributed;
using System.Net;

namespace DrugStoreWebsiteAuthen.Api.Middlewares
{
    public class TokenBlacklistMiddleware
    {
        private readonly RequestDelegate _next;

        public TokenBlacklistMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IDistributedCache cache)
        {
            // 1. Móc cái Token từ Header ra
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (!string.IsNullOrEmpty(token))
            {
                // 2. Ngó vào Redis xem Token này có nằm trong sổ đen không
                var isBlacklisted = await cache.GetStringAsync($"blacklist:{token}");

                if (!string.IsNullOrEmpty(isBlacklisted))
                {
                    // 3. Nếu có -> Đuổi thẳng cổ (Báo lỗi 401)
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"message\": \"Token has been revoked (Logout). Please log in again!\"}");
                    return;
                }
            }

            // 4. Nếu Token sạch sẽ, cho phép đi tiếp vào API
            await _next(context);
        }
    }
}
