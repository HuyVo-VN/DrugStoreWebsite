namespace DrugStoreWebSiteData.Application.DTOs.Request;

public class PaginationRequestDto
{
    public int PageNumber { get; set; } = 1;
    
    private int _pageSize = 4;
    public int PageSize
    {
        get => _pageSize;
    }
}