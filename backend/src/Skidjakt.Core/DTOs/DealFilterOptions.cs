namespace Skidjakt.Core.DTOs;

public record DealFilterOptions(
    IReadOnlyList<string> Agencies,
    IReadOnlyList<string> Countries,
    IReadOnlyList<string> Destinations,
    IReadOnlyList<string> TransportTypes,
    int MinPrice,
    int MaxPrice
);
