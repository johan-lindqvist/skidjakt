using AngleSharp;
using AngleSharp.Dom;
using Microsoft.Extensions.Logging;
using Skidjakt.Core.Entities;
using Skidjakt.Core.Interfaces;

namespace Skidjakt.Scraper.Scrapers;

public class SkilinkScraper : IDealScraper
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SkilinkScraper> _logger;

    public string Agency => "skilink";

    public SkilinkScraper(IHttpClientFactory httpClientFactory, ILogger<SkilinkScraper> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Deal>> ScrapeDealsAsync(CancellationToken ct)
    {
        var deals = new List<Deal>();

        try
        {
            var client = _httpClientFactory.CreateClient("Skilink");

            // Skilink uses an AJAX endpoint for package search
            var response = await client.GetStringAsync("https://www.skilink.se/sista-minuten/", ct);

            var config = Configuration.Default;
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(req => req.Content(response), ct);

            var dealElements = document.QuerySelectorAll(
                ".package-card, .deal-item, .search-result-item"
            );

            foreach (var element in dealElements)
            {
                try
                {
                    var deal = ParseDealElement(element);
                    if (deal != null)
                        deals.Add(deal);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse deal element from Skilink");
                }
            }

            _logger.LogInformation("Scraped {Count} deals from Skilink", deals.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scrape Skilink");
        }

        return deals;
    }

    private Deal? ParseDealElement(IElement element)
    {
        // Extract destination
        var destination = element.QuerySelector(".destination, h3, .title")?.TextContent?.Trim();
        if (string.IsNullOrEmpty(destination))
            return null;

        // Extract price
        var priceText = element.QuerySelector(".price, .amount")?.TextContent?.Trim();
        if (!TryParsePrice(priceText, out var price))
            return null;

        // Extract link
        var link = element.QuerySelector("a")?.GetAttribute("href") ?? "";
        if (!link.StartsWith("http"))
            link = "https://www.skilink.se" + link;

        // Generate external ID from link or content
        var externalId = link.GetHashCode().ToString("x8");

        // Extract country (often in breadcrumb or subtitle)
        var country = element.QuerySelector(".country, .subtitle")?.TextContent?.Trim() ?? "Okänt";

        // Extract dates
        var dateText = element.QuerySelector(".dates, .date-range")?.TextContent?.Trim();

        // Extract duration
        var durationText = element.QuerySelector(".duration, .nights")?.TextContent?.Trim();
        var durationNights = ParseDuration(durationText);

        // Extract original price for discount
        var originalPriceText = element
            .QuerySelector(".original-price, .was-price, del")
            ?.TextContent?.Trim();
        TryParsePrice(originalPriceText, out var originalPrice);

        return new Deal
        {
            Agency = "skilink",
            ExternalId = externalId,
            SourceUrl = link,
            Destination = destination,
            Country = country,
            PricePerPerson = price,
            OriginalPrice = originalPrice > 0 ? originalPrice : null,
            DiscountAmount = originalPrice > price ? originalPrice - price : null,
            DurationNights = durationNights > 0 ? durationNights : 7,
            DepartureDate = ParseDate(dateText),
            TransportType = element.QuerySelector(".transport")?.TextContent?.Trim(),
            IncludesFlight =
                element.TextContent?.Contains("flyg", StringComparison.OrdinalIgnoreCase) ?? false,
            IncludesLiftPass =
                element.TextContent?.Contains("liftkort", StringComparison.OrdinalIgnoreCase)
                ?? false,
            IncludesMeals =
                element.TextContent?.Contains("pension", StringComparison.OrdinalIgnoreCase)
                ?? false,
        };
    }

    private static bool TryParsePrice(string? text, out int price)
    {
        price = 0;
        if (string.IsNullOrEmpty(text))
            return false;

        var digits = new string(text.Where(c => char.IsDigit(c)).ToArray());
        return int.TryParse(digits, out price) && price > 0;
    }

    private static int ParseDuration(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;
        var digits = new string(text.Where(c => char.IsDigit(c)).ToArray());
        return int.TryParse(digits, out var n) ? n : 0;
    }

    private static DateOnly? ParseDate(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return null;
        // Try common Swedish date formats
        if (DateOnly.TryParse(text.Split('-', '–')[0].Trim(), out var date))
            return date;
        return null;
    }
}
