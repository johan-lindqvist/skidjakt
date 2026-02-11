using AngleSharp;
using AngleSharp.Dom;
using Microsoft.Extensions.Logging;
using Skidjakt.Core.Entities;
using Skidjakt.Core.Interfaces;

namespace Skidjakt.Scraper.Scrapers;

public class NortlanderScraper : IDealScraper
{
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly ILogger<NortlanderScraper> _logger;

	public string Agency => "nortlander";

	public NortlanderScraper(IHttpClientFactory httpClientFactory, ILogger<NortlanderScraper> logger)
	{
		_httpClientFactory = httpClientFactory;
		_logger = logger;
	}

	public async Task<IReadOnlyList<Deal>> ScrapeDealsAsync(CancellationToken ct)
	{
		var deals = new List<Deal>();

		try
		{
			var client = _httpClientFactory.CreateClient("Default");
			client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

			// Try multiple potential URL patterns for Nortlander deals
			var urls = new[]
			{
				"https://www.nortlander.se/sista-minuten/",
				"https://www.nortlander.se/erbjudanden/",
				"https://www.nortlander.se/skidresor/",
			};

			foreach (var url in urls)
			{
				try
				{
					var response = await client.GetStringAsync(url, ct);
					var config = Configuration.Default;
					var context = BrowsingContext.New(config);
					var document = await context.OpenAsync(req => req.Content(response), ct);

					var dealElements = document.QuerySelectorAll("[class*='trip'], [class*='deal'], [class*='offer'], [class*='package'], .product-card");

					foreach (var element in dealElements)
					{
						try
						{
							var deal = ParseDealElement(element, url);
							if (deal != null && !deals.Any(d => d.ExternalId == deal.ExternalId))
								deals.Add(deal);
						}
						catch (Exception ex)
						{
							_logger.LogWarning(ex, "Failed to parse deal element from Nortlander");
						}
					}
				}
				catch (HttpRequestException ex)
				{
					_logger.LogDebug(ex, "URL not available: {Url}", url);
				}
			}

			_logger.LogInformation("Scraped {Count} deals from Nortlander", deals.Count);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to scrape Nortlander");
		}

		return deals;
	}

	private Deal? ParseDealElement(IElement element, string baseUrl)
	{
		var destination = element.QuerySelector("h2, h3, h4, .title, .destination, [class*='title']")?.TextContent?.Trim();
		if (string.IsNullOrEmpty(destination))
			return null;

		var priceText = element.QuerySelector(".price, .amount, [class*='price']")?.TextContent?.Trim();
		if (!TryParsePrice(priceText, out var price))
			return null;

		var link = element.QuerySelector("a[href]")?.GetAttribute("href") ?? element.GetAttribute("href") ?? "";
		if (!string.IsNullOrEmpty(link) && !link.StartsWith("http"))
			link = "https://www.nortlander.se" + link;

		var externalId = !string.IsNullOrEmpty(link) ? link.GetHashCode().ToString("x8") : (destination + price).GetHashCode().ToString("x8");

		var country = element.QuerySelector(".country, [class*='country'], [class*='region']")?.TextContent?.Trim() ?? "Okänt";

		var durationText = element.QuerySelector("[class*='duration'], [class*='night'], .nights")?.TextContent?.Trim();
		var durationNights = ParseDuration(durationText);

		var dateText = element.QuerySelector("[class*='date'], .departure")?.TextContent?.Trim();

		var originalPriceText = element.QuerySelector("del, s, [class*='original'], [class*='was']")?.TextContent?.Trim();
		TryParsePrice(originalPriceText, out var originalPrice);

		var textContent = element.TextContent ?? "";

		// Try to extract departure city
		var departureCity =
			textContent.Contains("Stockholm", StringComparison.OrdinalIgnoreCase) ? "Stockholm"
			: textContent.Contains("Göteborg", StringComparison.OrdinalIgnoreCase) ? "Göteborg"
			: textContent.Contains("Malmö", StringComparison.OrdinalIgnoreCase) ? "Malmö"
			: null;

		return new Deal
		{
			Agency = "nortlander",
			ExternalId = externalId,
			SourceUrl = !string.IsNullOrEmpty(link) ? link : baseUrl,
			Destination = destination,
			Country = country,
			PricePerPerson = price,
			OriginalPrice = originalPrice > 0 ? originalPrice : null,
			DiscountAmount = originalPrice > price ? originalPrice - price : null,
			DurationNights = durationNights > 0 ? durationNights : 7,
			DepartureDate = ParseDate(dateText),
			DepartureCity = departureCity,
			TransportType =
				textContent.Contains("flyg", StringComparison.OrdinalIgnoreCase) ? "Flyg"
				: textContent.Contains("buss", StringComparison.OrdinalIgnoreCase) ? "Buss"
				: textContent.Contains("egen bil", StringComparison.OrdinalIgnoreCase) ? "Egen bil"
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
