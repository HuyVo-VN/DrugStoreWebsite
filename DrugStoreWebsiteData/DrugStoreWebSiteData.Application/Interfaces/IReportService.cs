using DrugStoreWebSiteData.Application.DTOs.Request;
using DrugStoreWebSiteData.Application.DTOs.Response;
using DrugStoreWebSiteData.Application.Common;
using DrugStoreWebSiteData.Application.DTOs;

namespace DrugStoreWebSiteData.Application.Interfaces;
public interface IReportService
{
    Task<ChartDataResponse> GetDashboardChartDataAsync(string entity, string statType, int? year, int? month);
    Task<string> ExportDynamicExcelAsync(string entity, string statType, int? year, int? month);
    Task<string> ExportDynamicPdfAsync(string entity, string statType, int? year, int? month);
}
