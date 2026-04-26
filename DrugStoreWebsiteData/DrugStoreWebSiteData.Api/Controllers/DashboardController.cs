using DrugStoreWebSiteData.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DrugStoreWebSiteData.Api.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IReportService _reportService;

        public DashboardController(IReportService reportService) => _reportService = reportService;

        [HttpGet("export-excel")]
        public async Task<IActionResult> ExportDynamicExcel([FromQuery] string entity, [FromQuery] string statType, [FromQuery] int? year, [FromQuery] int? month)
        {
            var downloadUrl = await _reportService.ExportDynamicExcelAsync(entity, statType, year, month);
            return Ok(new { downloadUrl });
        }

        [HttpGet("export-pdf")]
        public async Task<IActionResult> ExportDynamicPdf([FromQuery] string entity, [FromQuery] string statType, [FromQuery] int? year, [FromQuery] int? month)
        {
            var downloadUrl = await _reportService.ExportDynamicPdfAsync(entity, statType, year, month);
            return Ok(new { downloadUrl });
        }

        [HttpGet("chart-data")]
        public async Task<IActionResult> GetChartData(
        [FromQuery] string entity,
        [FromQuery] string statType,
        [FromQuery] int? year,
        [FromQuery] int? month)
        {
            var chartData = await _reportService.GetDashboardChartDataAsync(entity, statType, year, month);
            return Ok(new { data = chartData });
        }
    }
}
