namespace Skidjakt.Core.DTOs;

public record DealStats(
    int TotalDeals,
    Dictionary<string, int> DealsPerAgency,
    int AveragePrice,
    int LowestPrice,
    DateTime? LastUpdated
);
