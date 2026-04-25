using DrugStoreWebSiteData.Infrastructure.Persistence;
using DrugStoreWebSite.Infrastructure.Repositories;
using DrugStoreWebSiteData.Application.Interfaces;
using DrugStoreWebSiteData.Domain.Interfaces;
using DrugStoreWebSiteData.Infrastructure.Repositories;
using DrugStoreWebSiteData.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using dotenv.net;
using DrugStoreWebSiteData.Infrastructure.Services;
using DrugStoreWebSiteData.Application.DTOs.VnPay;
using DrugStoreWebSiteData.Application.Service;
using StackExchange.Redis;

DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ==========================================
// 🚀 CẤU HÌNH REDIS CACHE
// ==========================================
var redisConnectionString =
    builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";

try
{
    var multiplexer = await ConnectionMultiplexer.ConnectAsync(
        redisConnectionString + ",abortConnect=false"
    );

    builder.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);

    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "DrugStore_";
    });

    Console.WriteLine("✅ Redis connected.");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Redis failed: {ex.Message}");

    // fallback RAM cache
    builder.Services.AddDistributedMemoryCache();
}
// ==========================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidateIssuer = true,
        ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
        ValidateAudience = true,
        ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET")!)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

//Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Register DbContext with SQL Server provider
builder.Services.AddDbContext<DrugStoreDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Dependency Injection for Services and Repositories
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork_Repos>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IBannerRepository, BannerRepository>();
builder.Services.AddScoped<IBannerService, BannerService>();
builder.Services.AddScoped<ICollectionService, CollectionService>();
builder.Services.AddScoped<ICollectionRepository, CollectionRepository>();
builder.Services.AddScoped<IVnPayService, VnPayService>();
builder.Services.AddAuthorization();
builder.Services.Configure<VnPayConfig>(builder.Configuration.GetSection("Vnpay"));
builder.Services.AddScoped<IPhotoService, CloudinaryPhotoService>();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "My Service");
            c.RoutePrefix = string.Empty;
        }
    );
}

if (!app.Environment.IsDevelopment())
{
   // app.UseHttpsRedirection();
};

app.UseCors("AllowAll");
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<DrugStoreDbContext>();
        // Nếu sếp đã dùng lệnh Add-Migration trước đó thì dùng Migrate()
        // Nếu chưa dùng thì xài EnsureCreated() để nó tự ốp thẳng Entity thành bảng
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error when create database: {ex.Message}");
    }
}

app.Run();
