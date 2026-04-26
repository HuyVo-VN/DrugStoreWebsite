namespace DrugStoreWebSiteData.Application.DTOs.Response;

public class ChartDataResponse
{
    public List<string> Labels { get; set; } = new List<string>();
    public List<decimal> Data { get; set; } = new List<decimal>();
    public string ChartLabel { get; set; } = string.Empty;
}