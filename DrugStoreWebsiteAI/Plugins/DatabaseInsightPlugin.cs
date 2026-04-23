using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using OfficeOpenXml; // For Excel Export
using DrugStoreWebsiteAuthen.Infrastructure.Persistence;
using DrugStoreWebSiteData.Infrastructure.Persistence;

// using DrugStoreWebsiteData.Contexts; // Uncomment and use your actual DbContext namespace

namespace DrugStoreWebsiteAI.Plugins
{
    public class DatabaseInsightPlugin
    {
        private readonly DbContext _drugStoreDb;
        private readonly DbContext _authDb;
        private readonly IWebHostEnvironment _env;

        public DatabaseInsightPlugin(DbContext drugStoreDb, DbContext authDb, IWebHostEnvironment env)
        {
            _drugStoreDb = drugStoreDb;
            _authDb = authDb;
            _env = env; // Used to get the wwwroot path for saving Excel files
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

            var dbContext = targetDatabase.Equals("Auth", StringComparison.OrdinalIgnoreCase) ? _authDb : _drugStoreDb;

            try
            {
                // 1. LẤY CONNECTION NHƯNG KHÔNG DÙNG "USING"
                var connection = dbContext.Database.GetDbConnection();

                // 2. KIỂM TRA TRẠNG THÁI CỬA: Nếu đóng thì mở ra
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
                Console.WriteLine($"\n\n[LỖI DATABASE KINH HOÀNG]: {ex.Message}\n\n");
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

            var dbContext = targetDatabase.Equals("Auth", StringComparison.OrdinalIgnoreCase) ? _authDb : _drugStoreDb;

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
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
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
                var fileName = $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var exportFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "exports");
                Directory.CreateDirectory(exportFolder);
                var filePath = Path.Combine(exportFolder, fileName);

                await File.WriteAllBytesAsync(filePath, await package.GetAsByteArrayAsync());

                // Return Download Link
                return $"Export successful. File URL: http://localhost:5097/exports/{fileName}";

            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n\n[LỖI DATABASE KINH HOÀNG]: {ex.Message}\n\n");
                return $"Export Error: {ex.Message}";
            }
        }
    }
}