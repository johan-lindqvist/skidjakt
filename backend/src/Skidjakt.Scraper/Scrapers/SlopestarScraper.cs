using AngleSharp;
using AngleSharp.Dom;
using Microsoft.Extensions.Logging;
using Skidjakt.Core.Entities;
using Skidjakt.Core.Interfaces;

namespace Skidjakt.Scraper.Scrapers;

public class SlopestarScraper : IDealScraper
{
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly ILogger<SlopestarScraper> _logger;

	public string Agency => "slopestar";

	public SlopestarScraper(IHttpClientFactory httpClientFactory, ILogger<SlopestarScraper> logger)
	{
		_httpClientFactory = httpClientFactory;
		_logger = logger;
	}

	public async Task<IReadOnlyList<Deal>> ScrapeDealsAsync(CancellationToken ct)
	{
		var deals = new List<Deal>();

		try
		{
			// First try: HTTP + AngleSharp (faster, less resource-intensive)
			deals = await ScrapeWithHttpAsync(ct);

			if (deals.Count == 0)
			{
				_logger.LogInformation("No deals found via HTTP for Slopestar, site may require JS rendering");
				// Playwright fallback can be added here when needed
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to scrape Slopestar");
		}

		return deals;
	}

	private async Task<List<Deal>> ScrapeWithHttpAsync(CancellationToken ct)
	{
		var deals = new List<Deal>();
		var client = _httpClientFactory.CreateClient("Default");
		client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

		var response = await client.GetStringAsync("https://www.slopestar.se/sista-minuten-skidresor", ct);

		var config = Configuration.Default;
		var context = BrowsingContext.New(config);
		var document = await context.OpenAsync(req => req.Content(response), ct);

		var dealElements = document.QuerySelectorAll("[class*='trip'], [class*='deal'], [class*='product'], [class*='card'], .search-result");

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
				_logger.LogWarning(ex, "Failed to parse deal element from Slopestar");
			}
		}

		_logger.LogInformation("Scraped {Count} deals from Slopestar via HTTP", deals.Count);
		return deals;
	}

	private Deal? ParseDealElement(IElement element)
	{
		var destination = element.QuerySelector("h2, h3, h4, .destination, .title, [class*='title']")?.TextContent?.Trim();
		if (string.IsNullOrEmpty(destination))
			return null;

		var priceText = element.QuerySelector(".price, .amount, [class*='price']")?.TextContent?.Trim();
		if (!TryParsePrice(priceText, out var price))
			return null;

		var link = element.QuerySelector("a[href]")?.GetAttribute("href") ?? "";
		if (!string.IsNullOrEmpty(link) && !link.StartsWith("http"))
			link = "https://www.slopestar.se" + link;

		var externalId = !string.IsNullOrEmpty(link) ? link.GetHashCode().ToString("x8") : (destination + price).GetHashCode().ToString("x8");

		var country = element.QuerySelector(".country, [class*='country'], [class*='region']")?.TextContent?.Trim() ?? "Okänt";

		var durationText = element.QuerySelector("[class*='duration'], [class*='night']")?.TextContent?.Trim();
		var durationNights = ParseDuration(durationText);

		var dateText = element.QuerySelector("[class*='date'], .departure")?.TextContent?.Trim();

		var originalPriceText = element.QuerySelector("del, s, [class*='original']")?.TextContent?.Trim();
		TryParsePrice(originalPriceText, out var originalPrice);

		var textContent = element.TextContent ?? "";

		return new Deal
		{
			Agency = "slopestar",
			ExternalId = externalId,
			SourceUrl = !string.IsNullOrEmpty(link) ? link : "https://www.slopestar.se/sista-minuten-skidresor",
			Destination = destination,
			Country = country,
			PricePerPerson = price,
			OriginalPrice = originalPrice > 0 ? originalPrice : null,
			DiscountAmount = originalPrice > price ? originalPrice - price : null,
			DurationNights = durationNights > 0 ? durationNights : 7,
			DepartureDate = ParseDate(dateText),
			TransportType =
				textContent.Contains("flyg", StringComparison.OrdinalIgnoreCase) ? "Flyg"
				: textContent.Contains("buss", StringComparison.OrdinalIgnoreCase) ? "Buss"
				: null,
			IncludesFlight = textContent.Contains("flyg", StringComparison.OrdinalIgnoreCase),
			IncludesLiftPass =
				textContent.Contains("liftkort", StringComparison.OrdinalIgnoreCase) || textContent.Contains("skidpass", StringComparison.OrdinalIgnoreCase),
			IncludesMeals =
				textContent.Contains("pension", StringComparison.OrdinalIgnoreCase) || textContent.Contains("frukost", StringComparison.OrdinalIgnoreCase),
			MealDescription =
				textContent.Contains("halvpension", StringComparison.OrdinalIgnoreCase) ? "Halvpension"
				: textContent.Contains("helpension", StringComparison.OrdinalIgnoreCase) ? "Helpension"
				: textContent.Contains("frukost", StringComparison.OrdinalIgnoreCase) ? "Frukost"
				: null,
			IncludesTransfer = textContent.Contains("transfer", StringComparison.OrdinalIgnoreCase),
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
		if (DateOnly.TryParse(text.Split('-', '–')[0].Trim(), out var date))
			return date;
		return null;
	}
}
