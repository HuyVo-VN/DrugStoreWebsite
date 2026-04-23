using dotenv.net;
using OfficeOpenXml;
using Microsoft.SemanticKernel;
using DrugStoreWebsiteAI.Hubs;
using DrugStoreWebsiteAI.Plugins;
using DrugStoreWebsiteAuthen.Infrastructure.Persistence;
using DrugStoreWebSiteData.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
// Đã dọn dẹp các thư viện của OpenAI không cần thiết

var builder = WebApplication.CreateBuilder(args);

// 1. ĐỌC BIẾN MÔI TRƯỜNG TỪ .ENV
DotEnv.Load();
builder.Configuration.AddEnvironmentVariables();

// 2. CẤU HÌNH EXCEL
ExcelPackage.License.SetNonCommercialPersonal("DrugStore Project");

// 3. LẤY CẤU HÌNH AI (Mặc định xài Gemini 2.0 Flash)
var apiKey = builder.Configuration["GOOGLE_API_KEY"]!;
var modelId = builder.Configuration["GEMINI_MODEL"] ?? "gemini-2.0-flash";

// 4. KẾT NỐI DATABASE
var drugStoreConn = builder.Configuration["DRUGSTORE_CONNECTION"];
var authConn = builder.Configuration["AUTH_CONNECTION"];

if (string.IsNullOrEmpty(drugStoreConn) || string.IsNullOrEmpty(authConn))
{
    throw new Exception("🛑 Không tìm thấy chuỗi kết nối trong file .env!");
}

builder.Services.AddDbContext<DrugStoreDbContext>(options =>
    options.UseSqlServer(drugStoreConn));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(authConn));

// 5. CẤU HÌNH BỘ NÃO SEMANTIC KERNEL (GOOGLE GEMINI NATIVE)
builder.Services.AddScoped(sp =>
{
    var kernelBuilder = Kernel.CreateBuilder();

    // Khởi tạo não Gemini
    kernelBuilder.AddGoogleAIGeminiChatCompletion(modelId, apiKey);

    // Nạp khả năng đọc Excel
    kernelBuilder.Plugins.AddFromType<InventoryPlugin>();

    // Nạp khả năng chọc Database
    var drugStoreDb = sp.GetRequiredService<DrugStoreDbContext>();
    var authDb = sp.GetRequiredService<AppDbContext>();
    var env = sp.GetRequiredService<IWebHostEnvironment>();

    kernelBuilder.Plugins.AddFromObject(
        new DatabaseInsightPlugin(drugStoreDb, authDb, env),
        "DatabaseInsightPlugin"
    );

    return kernelBuilder.Build();
});

// 6. ĐĂNG KÝ CÁC DỊCH VỤ (SERVICES)
builder.Services.AddScoped<DrugStoreWebsiteAI.Services.IAiAgentService, DrugStoreWebsiteAI.Services.AiAgentService>();
builder.Services.AddScoped<DrugStoreWebsiteAI.Services.IExcelParserService, DrugStoreWebsiteAI.Services.ExcelParserService>();

builder.Services.AddControllers();

// 7. CẤU HÌNH CORS CHUẨN DUY NHẤT (Chống sập SignalR)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Bắt buộc phải có để chat Real-time
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

// ÉP DÙNG ĐÚNG CÁI CORS ĐÃ KHAI BÁO
app.UseCors("AllowAngular");

app.MapControllers();
app.MapHub<AiHub>("/ai-hub");
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();