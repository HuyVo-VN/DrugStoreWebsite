using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using DrugStoreWebSiteData.Application.Interfaces;
using DrugStoreWebSiteData.Domain.Interfaces;
using DrugStoreWebSiteData.Application.DTOs.Response;

namespace DrugStoreWebSiteData.Application.Services;

public class ReportService : IReportService
{
    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IMinIoService _minIoService;
    private readonly IDashboardRepository _dashboardRepo;

    public ReportService(
        IProductRepository productRepository,
        IOrderRepository orderRepository,
        IMinIoService minIoService,
        IDashboardRepository dashboardRepo)
    {
        _productRepository = productRepository;
        _orderRepository = orderRepository;
        _minIoService = minIoService;
        _dashboardRepo = dashboardRepo;

        // Thiết lập bản quyền cho QuestPDF (bản Community là miễn phí)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // ========================================================
    // 1. XUẤT EXCEL ĐỘNG (Dựa trên dữ liệu Dashboard)
    // ========================================================
    public async Task<string> ExportDynamicExcelAsync(string entity, string statType, int? year, int? month)
    {
        // Lấy đúng dữ liệu mà biểu đồ đang hiển thị
        var chartData = await GetDashboardChartDataAsync(entity, statType, year, month);

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Dashboard Report");

            // Đổ Header
            worksheet.Cell(1, 1).Value = "Item / Timeline";
            worksheet.Cell(1, 2).Value = chartData.ChartLabel;

            // Format Header cho đẹp
            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.BabyBlue;

            // Đổ Data vào các dòng tiếp theo
            for (int i = 0; i < chartData.Labels.Count; i++)
            {
                worksheet.Cell(i + 2, 1).Value = chartData.Labels[i];
                worksheet.Cell(i + 2, 2).Value = chartData.Data[i];
            }

            worksheet.Columns().AdjustToContents(); // Tự giãn độ rộng cột

            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                var content = stream.ToArray();

                // Đẩy lên MinIO
                return await _minIoService.UploadFileAsync(content, $"Report_Excel_{DateTime.Now:yyyyMMddHHmmss}.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            }
        }
    }

    // ========================================================
    // 2. XUẤT PDF ĐỘNG (Dựa trên dữ liệu Dashboard)
    // ========================================================
    public async Task<string> ExportDynamicPdfAsync(string entity, string statType, int? year, int? month)
    {
        // Lấy đúng dữ liệu mà biểu đồ đang hiển thị
        var chartData = await GetDashboardChartDataAsync(entity, statType, year, month);

        // Tạo Document bằng QuestPDF
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(1, Unit.Centimetre);
                page.Header().Text("PHARMACY DASHBOARD REPORT").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);

                page.Content().PaddingVertical(10).Column(col =>
                {
                    // Tiêu đề của loại báo cáo
                    col.Item().PaddingBottom(10).Text($"Report Type: {chartData.ChartLabel}").FontSize(14).Bold();

                    // Vẽ bảng
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2); // Cột chữ rộng hơn
                            columns.RelativeColumn(1); // Cột số
                        });

                        // Header của bảng
                        table.Header(header =>
                        {
                            header.Cell().BorderBottom(1).Padding(5).Text("Item / Timeline").Bold();
                            header.Cell().BorderBottom(1).Padding(5).Text("Value").Bold();
                        });

                        // Đổ Data vào bảng
                        for (int i = 0; i < chartData.Labels.Count; i++)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(chartData.Labels[i]);
                            // Format dạng số có dấu phẩy cho đẹp (VD: 1,000.00)
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(chartData.Data[i].ToString("N2"));
                        }
                    });
                });
            });
        });

        using (var stream = new MemoryStream())
        {
            document.GeneratePdf(stream);
            var content = stream.ToArray();

            // Đẩy lên MinIO
            return await _minIoService.UploadFileAsync(content, $"Report_Pdf_{DateTime.Now:yyyyMMddHHmmss}.pdf", "application/pdf");
        }
    }

    // ========================================================
    // 3. API TÍNH TOÁN DỮ LIỆU BIỂU ĐỒ (Dùng chung cho cả UI và Export)
    // ========================================================
    public async Task<ChartDataResponse> GetDashboardChartDataAsync(string entity, string statType, int? year, int? month)
    {
        var result = new ChartDataResponse();

        string timeLabel = "";
        if (year.HasValue && year > 0)
        {
            timeLabel = (month.HasValue && month > 0) ? $" in {month}/{year}" : $" in {year}";
        }
        else
        {
            timeLabel = " (All Time)";
        }

        switch (entity)
        {
            case "product":
                if (statType == "stock")
                {
                    result.ChartLabel = "Product Stock";
                    var data = await _dashboardRepo.GetProductStockAsync();
                    result.Labels = data.Keys.ToList();
                    result.Data = data.Values.Select(v => (decimal)v).ToList();
                }
                else if (statType == "top_selling")
                {
                    result.ChartLabel = $"Top Selling Products{timeLabel}";
                    var data = await _dashboardRepo.GetTopSellingProductsAsync(5, year, month);
                    result.Labels = data.Keys.ToList();
                    result.Data = data.Values.Select(v => (decimal)v).ToList();
                }
                break;

            case "category":
                if (statType == "prod_per_cat")
                {
                    result.ChartLabel = "Products per Category";
                    var data = await _dashboardRepo.GetProductsPerCategoryAsync();
                    result.Labels = data.Keys.ToList();
                    result.Data = data.Values.Select(v => (decimal)v).ToList();
                }
                else if (statType == "top_cat_selling")
                {
                    result.ChartLabel = $"Category Revenue ($){timeLabel}";
                    var data = await _dashboardRepo.GetTopCategorySellingAsync(5, year, month);
                    result.Labels = data.Keys.ToList();
                    result.Data = data.Values.ToList();
                }
                break;

            case "order":
                if (statType == "revenue_month")
                {
                    result.ChartLabel = $"Revenue ($){timeLabel}";
                    var data = await _dashboardRepo.GetRevenueByTimeAsync(year, month);
                    result.Labels = data.Keys.ToList();
                    result.Data = data.Values.ToList();
                }
                else if (statType == "order_month")
                {
                    result.ChartLabel = $"Orders Count{timeLabel}";
                    var data = await _dashboardRepo.GetOrdersByTimeAsync(year, month);
                    result.Labels = data.Keys.ToList();
                    result.Data = data.Values.Select(v => (decimal)v).ToList();
                }
                else if (statType == "order_status")
                {
                    result.ChartLabel = "Order Status";
                    var data = await _dashboardRepo.GetOrderStatusAsync();
                    result.Labels = data.Keys.ToList();
                    result.Data = data.Values.Select(v => (decimal)v).ToList();
                }
                break;
        }

        return result;
    }
}