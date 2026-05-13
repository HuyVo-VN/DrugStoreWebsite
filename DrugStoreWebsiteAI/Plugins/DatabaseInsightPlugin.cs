using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using OfficeOpenXml; // For Excel Export
using DrugStoreWebsiteAuthen.Infrastructure.Persistence;
using DrugStoreWebSiteData.Infrastructure.Persistence;
using Minio;
using Minio.DataModel.Args;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using DrugStoreWebSiteData.Domain.Entities;

// using DrugStoreWebsiteData.Contexts; // Uncomment and use your actual DbContext namespace

namespace DrugStoreWebsiteAI.Plugins
{
    public class DatabaseInsightPlugin
    {
        private readonly DrugStoreDbContext _drugStoreDb;
        private readonly AppDbContext _authDb;
        private readonly IWebHostEnvironment _env;
        private readonly IMinioClient _minioClient;
        private readonly IDistributedCache _cache;

        public DatabaseInsightPlugin(
                        DrugStoreDbContext drugStoreDb,
                        AppDbContext authDb,
                        IWebHostEnvironment env,
                        IMinioClient minioClient,
                        IDistributedCache cache)
        {
            _drugStoreDb = drugStoreDb;
            _authDb = authDb;
            _env = env;
            _minioClient = minioClient;
            _cache = cache;
        }

        [KernelFunction("get_database_schema")]
        [Description("Provides a structured list of available Administrative Views. Use this to identify which view contains the data needed for the Admin's request. NEVER query raw tables directly.")]
        public string GetDatabaseSchema()
        {
            // The schema map now accurately reflects the customized Views across both databases.
            var schemaInfo = @"
            =========================================
            [DATABASE: DrugStoreDb] - Business & Inventory
            =========================================
            1. VIEW: vw_ProductSummary
               - Use for: Checking stock, prices, categories, and sales performance.
               - Columns: ProductId, ProductName, CategoryName, Price, Stock, SoldQuantity, DiscountPercent, IsActive, SaleStock, SaleSold, Specifications

            2. VIEW: vw_OrderSummary
               - Use for: Calculating revenue, checking order status, and tracking dates.
               - Columns: OrderId, UserId, OrderDate, TotalAmount, Status, ShippingAddress, PhoneNumber

            3. VIEW: vw_PendingCarts
               - Use for: Checking items currently in users' shopping carts.
               - Columns: CartId, UserId, ProductId, Quantity

            =========================================
            [DATABASE: AuthDb] - Security & Accounts
            =========================================
            4. VIEW: vw_UserSummary
               - Use for: Retrieving basic user profiles and checking lockout/security status.
               - Columns: UserId, UserName, FullName, Email, PhoneNumber, Gender, DateOfBirth, EmailConfirmed, AccessFailedCount, LockoutEnd

            5. VIEW: vw_UserRolesInfo
               - Use for: Identifying user permissions (e.g., finding all 'Admin' or 'Customer' accounts).
               - Columns: UserName, Email, RoleName
            ";

            return schemaInfo;
        }

        [KernelFunction("execute_readonly_sql_query")]
        [Description("Executes a SQL SELECT query for CHAT RESPONSES. It automatically limits to 20 rows. Use this when the user asks a question that requires a short answer or summary.")]
        public async Task<string> ExecuteReadOnlySqlQueryAsync(
            [Description("Target database: 'DrugStore' or 'Auth'")] string targetDatabase,
            [Description("The SQL SELECT query")] string sqlQuery)
        {
            if (!sqlQuery.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                return "Error: Only SELECT queries are allowed.";

            DbContext dbContext = targetDatabase.Equals("Auth", StringComparison.OrdinalIgnoreCase)
            ? _authDb
            : _drugStoreDb;

            try
            {
                var connection = dbContext.Database.GetDbConnection();

                bool wasClosed = connection.State == ConnectionState.Closed;
                if (wasClosed)
                {
                    await connection.OpenAsync();
                }

                using var command = connection.CreateCommand();
                command.CommandText = sqlQuery;

                using var reader = await command.ExecuteReaderAsync();
                var results = new List<Dictionary<string, object>>();

                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    results.Add(row);

                    // GUARDRAIL: Ngắt ngay nếu quá 20 dòng để cứu Token
                    if (results.Count >= 20) break;
                }

                // 3. KHÉP CỬA LẠI ĐÚNG TRẠNG THÁI BAN ĐẦU
                if (wasClosed)
                {
                    await connection.CloseAsync();
                }

                return JsonSerializer.Serialize(results);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n\n[DATABASE ERROR]: {ex.Message}\n\n");
                return $"SQL Error: {ex.Message}";
            }
        }

        [KernelFunction("export_data_to_excel")]
        [Description("Executes a SQL SELECT query and EXPORTS the entire result to an Excel file. Use this ONLY when the user explicitly requests to 'xuất báo cáo', 'tải file', or when dealing with large datasets.")]
        public async Task<string> ExportDataToExcelAsync(
            [Description("Target database: 'DrugStore' or 'Auth'")] string targetDatabase,
            [Description("The SQL SELECT query to export")] string sqlQuery)
        {
            if (!sqlQuery.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                return "Error: Only SELECT queries are allowed.";

            DbContext dbContext = targetDatabase.Equals("Auth", StringComparison.OrdinalIgnoreCase)
            ? _authDb
            : _drugStoreDb;

            try
            {
                using var connection = dbContext.Database.GetDbConnection();
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = sqlQuery;

                using var reader = await command.ExecuteReaderAsync();
                var dataTable = new DataTable();
                dataTable.Load(reader); // Load all data for export

                // Generate Excel File
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Report");
                worksheet.Cells["A1"].LoadFromDataTable(dataTable, true);

                // Format Header
                using (var range = worksheet.Cells[1, 1, 1, dataTable.Columns.Count])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                }
                worksheet.Cells.AutoFitColumns();

                // Save File
                // --- LƯU FILE LÊN MINIO CLOUD ---
                var fileName = $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var bucketName = "ai-reports";

                // 1. Chuyển file Excel thành Stream 
                using var excelStream = new MemoryStream(await package.GetAsByteArrayAsync());
                excelStream.Position = 0;

                // 2. Kiểm tra xem Bucket (Thư mục) đã tồn tại chưa, chưa có thì tạo mới
                bool found = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
                if (!found)
                {
                    await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
                }

                // 👉 ÉP BUỘC GẮN POLICY PUBLIC CHO BUCKET (Bỏ ra ngoài khối IF để đảm bảo rổ nào cũng được mở khóa)
                var policy = $"{{\"Version\":\"2012-10-17\",\"Statement\":[{{\"Action\":[\"s3:GetObject\"],\"Effect\":\"Allow\",\"Principal\":{{\"AWS\":[\"*\"]}},\"Resource\":[\"arn:aws:s3:::{bucketName}/*\"]}}]}}";
                await _minioClient.SetPolicyAsync(new SetPolicyArgs().WithBucket(bucketName).WithPolicy(policy));

                // 3. Đẩy file lên mây
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(fileName)
                    .WithStreamData(excelStream)
                    .WithObjectSize(excelStream.Length)
                    .WithContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                await _minioClient.PutObjectAsync(putObjectArgs);

                // 👉 4. TRẢ VỀ LINK PUBLIC TRỰC TIẾP (Tuyệt đối KHÔNG dùng Presigned Url hay Replace)
                string publicHost = "http://localhost:9000"; // Mặc định cho lúc sếp code máy nhà

                if (!_env.IsDevelopment())
                {
                    // Chạy trên VPS thì gắn tên miền thật
                    publicHost = "https://drugstore-huyvo.duckdns.org/minio-files";
                }

                string finalUrl = $"{publicHost}/{bucketName}/{fileName}";

                return $"Export successful. [Click here to download the Excel report.]({finalUrl})";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n\n[HORRIFYING DATABASE ERROR]: {ex.Message}\n\n");
                return $"Export Error: {ex.Message}";
            }
        }

        [KernelFunction("import_approved_inventory_data")]
        [Description("Use this function ONLY when the Admin AGREES to import data from the Excel file into the warehouse. You must find the Session Code (cacheKey) in the chat history and pass it here.")]
        public async Task<string> ImportApprovedDataAsync(
            [Description("Temporary session code in the format 'excel_import_...'")]
            string cacheKey,
            [Description("GUID of the category selected by the Admin for imported products. Retrieve it using the get_all_categories function.")]
            string categoryId,
            [Description("Additional instructions from the Admin for handling missing data. Example: 'If price is missing set it to 100000, if stock is missing set it to 50'.")]
            string instructions)
        {
            // Lấy Cache từ Environment
            var jsonData = await _cache.GetStringAsync(cacheKey);

            if (string.IsNullOrEmpty(jsonData)) return "Error: File expired or incorrect session code.";
            if (!Guid.TryParse(categoryId, out Guid catId)) return "Error: Invalid CategoryId. Please ask the Admin.";

            try
            {
                // Chuyển đổi JSON thành danh sách các phần tử động
                using var jsonDoc = JsonDocument.Parse(jsonData);
                var records = jsonDoc.RootElement.EnumerateArray();
                var productsToAdd = new List<Product>();

                string lowerInstructions = instructions.ToLower();

                // Quét từng dòng dữ liệu Excel đã được AI chuẩn hóa
                foreach (var record in records)
                {
                    // --- XỬ LÝ BASE INFO ---
                    string productName = "The product is unnamed.";
                    string description = string.Empty;
                    decimal price = 0;
                    int stock = 0;

                    void CheckAndAssign(string k, string v)
                    {
                        if (k.Contains("tên") || k.Contains("name")) productName = v;
                        else if (k.Contains("giá") || k.Contains("price")) decimal.TryParse(v, out price);
                        else if (k.Contains("tồn") || k.Contains("số lượng") || k.Contains("stock") || k.Contains("quantity") || k.Contains("quantities")) int.TryParse(v, out stock);
                        else if (k.Contains("mô tả") || k.Contains("description") || k.Contains("chi tiết") || k.Contains("detail")) description = v;
                    }

                    // QUÉT LỚP NGOÀI CÙNG (Đề phòng JSON phẳng)
                    foreach (var prop in record.EnumerateObject())
                    {
                        if (prop.Value.ValueKind != JsonValueKind.Object && prop.Value.ValueKind != JsonValueKind.Array)
                        {
                            CheckAndAssign(prop.Name.ToLower(), prop.Value.ToString());
                        }
                    }

                    // QUÉT TRONG BASE INFO (Cấu trúc chuẩn)
                    if (record.TryGetProperty("BaseInfo", out var baseInfo) && baseInfo.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in baseInfo.EnumerateObject())
                        {
                            CheckAndAssign(prop.Name.ToLower(), prop.Value.ToString());
                        }
                    }

                    // QUÉT VỚT TRONG SPECIFICATIONS (Phòng hờ bị nhóm nhầm thành thông số kỹ thuật)
                    if (string.IsNullOrEmpty(description) && record.TryGetProperty("Specifications", out var fallbackSpecs) && fallbackSpecs.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in fallbackSpecs.EnumerateObject())
                        {
                            var subKey = prop.Name.ToLower();
                            if (subKey.Contains("mô tả") || subKey.Contains("description") || subKey.Contains("chi tiết"))
                            {
                                description = prop.Value.ToString();
                            }
                        }
                    }
                    // --- BÙ DỮ LIỆU THIẾU TỪ LỜI DẶN CỦA ADMIN ---
                    // Nếu không tìm thấy giá trong Excel, quét xem Admin có dặn giá mặc định không
                    if (price == 0 && (lowerInstructions.Contains("giá") || lowerInstructions.Contains("price")))
                    {
                        var words = lowerInstructions.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var word in words)
                        {
                            if (decimal.TryParse(word, out decimal parsedPrice))
                            {
                                price = parsedPrice;
                                break;
                            }
                        }
                    }
                    // Nếu không có tồn kho trong Excel, quét xem Admin có dặn mặc định không
                    if (stock == 0 &&
                    (
                        lowerInstructions.Contains("tồn") ||
                        lowerInstructions.Contains("số lượng") ||
                        lowerInstructions.Contains("stock") ||
                        lowerInstructions.Contains("quantity")
                    ))
                    {
                        var words = lowerInstructions.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var word in words)
                        {
                            if (int.TryParse(word, out int parsedStock))
                            {
                                stock = parsedStock;
                                break;
                            }
                        }
                    }
                    // Nếu không có mô tả, dừng toàn bộ tiến trình và báo lỗi cho Admin
                    if (string.IsNullOrEmpty(description) || description.Length < 5)
                    {
                        return $"❌ Inventory error: Product '{productName}' detailed descriptive information is missing. Please check the Excel file again.";
                    }

                    // --- XỬ LÝ SALE INFO ---
                    int discountPercent = 0;
                    if (record.TryGetProperty("SaleInfo", out var saleInfo))
                    {
                        foreach (var prop in saleInfo.EnumerateObject())
                        {
                            var key = prop.Name.ToLower();
                            var val = prop.Value.GetString() ?? "";

                            if (key.Contains("giảm") || key.Contains("khuyến mãi") || key.Contains("sale") || key.Contains("promo"))
                            {
                                // Xóa dấu % nếu có rồi ép kiểu
                                int.TryParse(val.Replace("%", "").Trim(), out discountPercent);
                            }
                        }
                    }

                    // --- XỬ LÝ SPECIFICATIONS (Gom toàn bộ thành chuỗi JSON) ---
                    string specsJson = "[]";
                    if (record.TryGetProperty("Specifications", out var specs))
                    {
                        try
                        {
                            var specList = new List<object>();

                            // Case 1: AI returns an Object of type {"Component": "...", "Usage": "..."}
                            if (specs.ValueKind == JsonValueKind.Object)
                            {
                                foreach (var prop in specs.EnumerateObject())
                                {
                                    specList.Add(new { key = prop.Name, value = prop.Value.GetString() ?? "" });
                                }
                            }
                            // Case 2: AI returns a pre-made array.
                            else if (specs.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var item in specs.EnumerateArray())
                                {
                                    if (item.ValueKind == JsonValueKind.Object)
                                    {
                                        // Linh hoạt bắt key-value
                                        string k = item.TryGetProperty("key", out var kProp) ? kProp.GetString() ?? "" :
                                                   item.TryGetProperty("Key", out var kProp2) ? kProp2.GetString() ?? "" : "";
                                        string v = item.TryGetProperty("value", out var vProp) ? vProp.GetString() ?? "" :
                                                   item.TryGetProperty("Value", out var vProp2) ? vProp2.GetString() ?? "" : "";

                                        if (!string.IsNullOrEmpty(k))
                                        {
                                            specList.Add(new { key = k, value = v });
                                        }
                                    }
                                }
                            }

                            // Use UnsafeRelaxedJsonEscaping to force C# to retain Vietnamese characters with diacritics.
                            var options = new JsonSerializerOptions
                            {
                                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                            };

                            specsJson = JsonSerializer.Serialize(specList, options);
                        }
                        catch (Exception)
                        {
                            // Fallback an toàn nếu có lỗi parse
                            specsJson = "[]";
                        }
                    }

                    // --- XỬ LÝ ẢNH ---
                    string imageUrl = string.Empty;
                    if (record.TryGetProperty("Images", out var images) && images.GetArrayLength() > 0)
                    {
                        imageUrl = images[0].GetString() ?? string.Empty;
                    }

                    // CHỐNG TRÙNG LẶP: Kiểm tra xem sản phẩm đã có trong DB chưa
                    var existingProduct = await _drugStoreDb.Products
                                            .FirstOrDefaultAsync(p => p.Name.ToLower() == productName.ToLower());

                    if (existingProduct != null)
                    {
                        // Nếu có rồi -> Cập nhật tồn kho và giá mới
                        existingProduct.AddStockAndUpdatePrice(stock, price);

                    }
                    else
                    {
                        // Nếu chưa có -> Tạo mới hoàn toàn
                        var newProduct = new Product(
                            name: productName,
                            description: description,
                            price: price,
                            stock: stock,
                            categoryId: catId,
                            discountPercent: discountPercent,
                            discountEndDate: discountPercent > 0 ? DateTime.UtcNow.AddDays(7) : null,
                            saleStock: discountPercent > 0 ? stock : 0,
                            specifications: specsJson
                        );
                        newProduct.ImageUrl = imageUrl;
                        productsToAdd.Add(newProduct);
                    }
                }

                // 6. Lưu xuống Database Data
                if (productsToAdd.Any())
                {
                    _drugStoreDb.Products.AddRange(productsToAdd);
                }

                await _drugStoreDb.SaveChangesAsync();

                // 7. Dọn dẹp RAM
                await _cache.RemoveAsync(cacheKey);

                return $"Confirmation successful! The {productsToAdd.Count} product has been processed and saved to the database. The instruction '{instructions}' has been recorded.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n\n[Error when IMPORT to DB]: {ex.ToString()}\n\n");
                return $"Error when importing inventory: {ex.Message}";
            }
        }

        [KernelFunction("get_all_categories")]
        [Description("Get a list of existing Categories in the system along with their IDs. Use this function to advise the Admin on which category to put a product into.")]
        public async Task<string> GetAllCategoriesAsync()
        {
            try
            {
                // Trả về tên và ID để AI biết đường gọi hàm Import
                var categories = await _drugStoreDb.Categories
                    .Select(c => new { c.Id, c.Name })
                    .ToListAsync();

                if (!categories.Any()) return "No categories have been created in the current system.";
                return JsonSerializer.Serialize(categories);
            }
            catch (Exception ex)
            {
                return $"Error when querying Categories: {ex.Message}";
            }
        }


    }
}