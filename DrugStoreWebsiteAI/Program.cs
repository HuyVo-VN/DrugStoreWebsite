using dotenv.net;
using OfficeOpenXml;
using Microsoft.SemanticKernel;
using DrugStoreWebsiteAI.Hubs;
using DrugStoreWebsiteAI.Plugins;
using DrugStoreWebsiteAuthen.Infrastructure.Persistence;
using DrugStoreWebSiteData.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minio; // 👈 Đã thêm thư viện MinIO

var builder = WebApplication.CreateBuilder(args);

// 1. ĐỌC BIẾN MÔI TRƯỜNG TỪ .ENV
DotEnv.Load();
builder.Configuration.AddEnvironmentVariables();

// 2. CẤU HÌNH EXCEL
ExcelPackage.License.SetNonCommercialPersonal("DrugStore Project");

// 3. LẤY CẤU HÌNH AI 
var apiKey = builder.Configuration["GOOGLE_API_KEY"]!;
var modelId = builder.Configuration["GEMINI_MODEL"] ?? "gemini-1.5-flash";

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

// =========================================================
// ĐĂNG KÝ MINIO CLIENT (BẢN FIX LỖI THIẾU HÀM ADDMINIO)
// =========================================================
var minioEndpoint = builder.Configuration["Minio:Endpoint"];
var minioAccessKey = builder.Configuration["Minio:AccessKey"];
var minioSecretKey = builder.Configuration["Minio:SecretKey"];

// Dùng AddSingleton thủ công để đảm bảo 100% C# nhận diện được MinIO
builder.Services.AddSingleton<IMinioClient>(sp =>
{
    return new MinioClient()
        .WithEndpoint(minioEndpoint)
        .WithCredentials(minioAccessKey, minioSecretKey)
        .Build();
});

// =========================================================
// 5. CẤU HÌNH BỘ NÃO SEMANTIC KERNEL (ĐÃ SẮP XẾP LẠI TRẬT TỰ)
// =========================================================
builder.Services.AddScoped(sp =>
{
    var kernelBuilder = Kernel.CreateBuilder();

    // Khởi tạo não Gemini
    kernelBuilder.AddGoogleAIGeminiChatCompletion(modelId, apiKey);

    // BƯỚC 1: LẤY TẤT CẢ ĐỒ NGHỀ RA TRƯỚC (Phải lấy ra rồi mới xài được)
    var drugStoreDb = sp.GetRequiredService<DrugStoreDbContext>();
    var authDb = sp.GetRequiredService<AppDbContext>();
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var minioClient = sp.GetRequiredService<IMinioClient>();

    // BƯỚC 2: TIÊM TOÀN BỘ VÀO PLUGIN CÙNG MỘT LÚC (Xóa bỏ phần đăng ký trùng lặp)
    kernelBuilder.Plugins.AddFromObject(
        new DatabaseInsightPlugin(drugStoreDb, authDb, env, minioClient),
        "DatabaseInsightPlugin"
    );

    return kernelBuilder.Build();
});

// 6. ĐĂNG KÝ CÁC DỊCH VỤ (SERVICES)
builder.Services.AddScoped<DrugStoreWebsiteAI.Services.IAiAgentService, DrugStoreWebsiteAI.Services.AiAgentService>();
builder.Services.AddScoped<DrugStoreWebsiteAI.Services.IExcelParserService, DrugStoreWebsiteAI.Services.ExcelParserService>();

builder.Services.AddControllers();

// 7. CẤU HÌNH CORS CHUẨN DUY NHẤT 
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
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

// ÉP DÙNG ĐÚNG CÁI CORS ĐÃ KHAI BÁO
app.UseCors("AllowAngular");

app.MapControllers();
app.MapHub<AiHub>("/ai-hub");
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();