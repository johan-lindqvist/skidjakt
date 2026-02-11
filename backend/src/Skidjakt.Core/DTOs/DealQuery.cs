namespace Skidjakt.Core.DTOs;

public class DealQuery
{
    public string? Search { get; set; }
    public string[]? Agencies { get; set; }
    public string[]? Countries { get; set; }
    public string[]? Destinations { get; set; }
    public int? MinPrice { get; set; }
    public int? MaxPrice { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string[]? TransportTypes { get; set; }
    public string? SortBy { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
}
