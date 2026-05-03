using dotenv.net;
using OfficeOpenXml;
using Microsoft.SemanticKernel;
using DrugStoreWebsiteAI.Hubs;
using DrugStoreWebsiteAI.Plugins;
using DrugStoreWebsiteAuthen.Infrastructure.Persistence;
using DrugStoreWebSiteData.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minio;

var builder = WebApplication.CreateBuilder(args);

// Read .ENV
DotEnv.Load();
builder.Configuration.AddEnvironmentVariables();

// Get EXCEL config
ExcelPackage.License.SetNonCommercialPersonal("DrugStore Project");

// Get AI Config
var apiKey = builder.Configuration["GOOGLE_API_KEY"]!;
var modelId = builder.Configuration["GEMINI_MODEL"] ?? "gemma-4-31b-it";

// Connect DATABASE
var drugStoreConn = builder.Configuration["DRUGSTORE_CONNECTION"];
var authConn = builder.Configuration["AUTH_CONNECTION"];

if (string.IsNullOrEmpty(drugStoreConn) || string.IsNullOrEmpty(authConn))
{
    throw new Exception("🛑 Connection string not found in .env file!");
}

builder.Services.AddDbContext<DrugStoreDbContext>(options =>
    options.UseSqlServer(drugStoreConn));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(authConn));

// ====================
// SIGN IN MINIO CLIENT
// ====================
var minioEndpoint = builder.Configuration["Minio:Endpoint"];
var minioAccessKey = builder.Configuration["Minio:AccessKey"];
var minioSecretKey = builder.Configuration["Minio:SecretKey"];

builder.Services.AddSingleton<IMinioClient>(sp =>
{
    return new MinioClient()
        .WithEndpoint(minioEndpoint)
        .WithCredentials(minioAccessKey, minioSecretKey)
        .Build();
});

// ==================================
//  SEMANTIC KERNEL CONFIGURATION
// ==================================
builder.Services.AddScoped(sp =>
{
    var kernelBuilder = Kernel.CreateBuilder();

    // Start Gemini
    kernelBuilder.AddGoogleAIGeminiChatCompletion(modelId, apiKey);

    // Get all funtion
    var drugStoreDb = sp.GetRequiredService<DrugStoreDbContext>();
    var authDb = sp.GetRequiredService<AppDbContext>();
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var minioClient = sp.GetRequiredService<IMinioClient>();

    kernelBuilder.Plugins.AddFromObject(
        new DatabaseInsightPlugin(drugStoreDb, authDb, env, minioClient),
        "DatabaseInsightPlugin"
    );

    return kernelBuilder.Build();
});

// sign up services
builder.Services.AddScoped<DrugStoreWebsiteAI.Services.IAiAgentService, DrugStoreWebsiteAI.Services.AiAgentService>();
builder.Services.AddScoped<DrugStoreWebsiteAI.Services.IExcelParserService, DrugStoreWebsiteAI.Services.ExcelParserService>();

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://drugstore-huyvo.duckdns.org")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ----- MIDDLEWARE PIPELINE -----
app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles();

app.UseCors("AllowAngular");

app.MapControllers();
app.MapHub<AiHub>("/ai-hub");
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();