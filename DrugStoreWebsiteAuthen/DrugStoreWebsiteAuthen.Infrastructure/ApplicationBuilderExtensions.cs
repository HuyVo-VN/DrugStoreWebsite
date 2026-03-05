using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using DrugStoreWebsiteAuthen.Infrastructure.Persistence;
using DrugStoreWebsiteAuthen.Application.Interfaces;

public static class ApplicationBuilderExtensions
{
    public static async Task SeedUsersAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        await userService.SeedAdminUserAsync("admin", "Admin@123");

    }

    public static void EnsureSchemaCreated(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Database.EnsureCreated();
    }
}
