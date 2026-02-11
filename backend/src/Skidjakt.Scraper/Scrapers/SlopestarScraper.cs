using System.Globalization;
using System.Text;
using AngleSharp;
using AngleSharp.Dom;
using Microsoft.Extensions.Logging;
using Skidjakt.Core.Entities;
using Skidjakt.Core.Interfaces;

namespace Skidjakt.Scraper.Scrapers;

public class SlopestarScraper : IDealScraper
{
	private const string AjaxUrl = "https://www.slopestar.se/includes/ajax_show-earlybookings.php";

	private const int MaxPages = 20;

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
		var allDeals = new List<Deal>();

		try
		{
			var client = _httpClientFactory.CreateClient("Default");
			client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

			var config = Configuration.Default;
			var context = BrowsingContext.New(config);

			for (int page = 1; page <= MaxPages; page++)
			{
				ct.ThrowIfCancellationRequested();

				var url =
					$"{AjaxUrl}?earlybookings_destination=&earlybookings_transport=&earlybookings_travelperiods=&earlybookings_pax=&earlybookings_page_id=lastminute&track_page={page}";
				var response = await client.GetAsync(url, ct);
				var bytes = await response.Content.ReadAsByteArrayAsync(ct);
				var charset = response.Content.Headers.ContentType?.CharSet;
				var encoding = !string.IsNullOrEmpty(charset) ? Encoding.GetEncoding(charset) : Encoding.Latin1;
				var html = encoding.GetString(bytes);

				if (string.IsNullOrWhiteSpace(html))
					break;

				var document = await context.OpenAsync(req => req.Content(html), ct);
				var deals = ParseDocument(document);

				if (deals.Count == 0)
					break;

				allDeals.AddRange(deals);
				_logger.LogDebug("Slopestar page {Page}: {Count} deals", page, deals.Count);

				// Be polite
				await Task.Delay(500, ct);
			}

			// Deduplicate by ExternalId (later pages may have overlapping deals)
			var deduplicated = allDeals.GroupBy(d => d.ExternalId).Select(g => g.Last()).ToList();

			_logger.LogInformation(
				"Scraped {Count} deals from Slopestar ({Raw} raw, {Dupes} duplicates removed)",
				deduplicated.Count,
				allDeals.Count,
				allDeals.Count - deduplicated.Count
			);

			return deduplicated;
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to scrape Slopestar");
		}

		return allDeals;
	}

	internal static List<Deal> ParseDocument(IDocument document)
	{
		var deals = new List<Deal>();
		var containers = document.QuerySelectorAll("div.st-list-discount-container");

		foreach (var container in containers)
		{
			var deal = ParseContainer(container);
			if (deal != null)
				deals.Add(deal);
		}

		return deals;
	}

	internal static Deal? ParseContainer(IElement container)
	{
		// Date: DD.MM.YYYY
		var dateText = container.QuerySelector(".col-md-2-mbj-afrejse .data-discount-tabel")?.TextContent?.Trim();
		var departureDate = ParseDate(dateText);

		// Duration (days)
		var durationText = container.QuerySelector(".col-md-2-mbj-dage .data-discount-tabel")?.TextContent?.Trim();
		int.TryParse(durationText, out var durationNights);

		// Destination (text after flag image)
		var destElement = container.QuerySelector(".col-md-2-mbj-rejsemaal .data-discount-tabel");
		var destination = destElement?.TextContent?.Trim();
		if (string.IsNullOrEmpty(destination))
			return null;

		// Country from flag icon class or mobile h3
		var country = ExtractCountry(container, destElement);

		// Transport
		var transportText = container.QuerySelector(".col-md-2-mbj-transport .data-discount-tabel")?.TextContent?.Trim();

		// Accommodation
		var accommodationSpan = container.QuerySelector(".col-md-2-mbj-indkvartering .data-discount-tabel span[title]");
		var accommodation = accommodationSpan?.GetAttribute("title") ?? accommodationSpan?.TextContent?.Trim();

		// Price (current price in <strong>)
		var priceText = container.QuerySelector(".col-md-2-mbj-pris .data-discount-tabel strong")?.TextContent?.Trim();
		if (!TryParsePrice(priceText, out var price))
			return null;

		// Original price in <s>
		var originalPriceText = container.QuerySelector(".col-md-2-mbj-pris s")?.TextContent?.Trim();
		TryParsePrice(originalPriceText, out var originalPrice);

		// Discount text
		var discountText = container.QuerySelector(".data-discount-tabel-dicount-price")?.TextContent?.Trim();

		// Booking URL
		var bookingUrl = container.QuerySelector("a.btn-booking")?.GetAttribute("href") ?? "";
		if (!string.IsNullOrEmpty(bookingUrl) && !bookingUrl.StartsWith("http"))
			bookingUrl = "https://www.slopestar.se" + bookingUrl;

		// External ID from booking URL params
		var externalId = ExtractExternalId(bookingUrl, destination, price);

		// Price includes
		var includesText = container.QuerySelector(".price-includes")?.TextContent ?? "";
		var includesLiftPass = includesText.Contains("liftkort", StringComparison.OrdinalIgnoreCase);
		var includesTransfer = includesText.Contains("transfer", StringComparison.OrdinalIgnoreCase);
		var includesMeals =
			includesText.Contains("pension", StringComparison.OrdinalIgnoreCase) || includesText.Contains("frukost", StringComparison.OrdinalIgnoreCase);
		var mealDescription =
			includesText.Contains("halvpension", StringComparison.OrdinalIgnoreCase) ? "Halvpension"
			: includesText.Contains("helpension", StringComparison.OrdinalIgnoreCase) ? "Helpension"
			: includesText.Contains("frukost", StringComparison.OrdinalIgnoreCase) ? "Frukost"
			: null;

		var includesFlight = transportText?.Contains("Flyg", StringComparison.OrdinalIgnoreCase) ?? false;

		return new Deal
		{
			Agency = "slopestar",
			ExternalId = externalId,
			SourceUrl = !string.IsNullOrEmpty(bookingUrl) ? bookingUrl : "https://www.slopestar.se/sista-minuten-skidresor",
			Destination = destination,
			Country = country,
			PricePerPerson = price,
			OriginalPrice = originalPrice > 0 ? originalPrice : null,
			DiscountAmount = originalPrice > price ? originalPrice - price : null,
			DurationNights = durationNights > 0 ? durationNights : 7,
			DepartureDate = departureDate,
			AccommodationName = accommodation,
			TransportType = transportText,
			IncludesFlight = includesFlight,
			IncludesLiftPass = includesLiftPass,
			IncludesMeals = includesMeals,
			MealDescription = mealDescription,
			IncludesTransfer = includesTransfer,
		};
	}

	internal static string ExtractCountry(IElement container, IElement? destElement)
	{
		// Try flag icon class: icon-flag-at → Österrike
		if (destElement != null)
		{
			var flagImg = destElement.QuerySelector("img[class*='icon-flag-']");
			if (flagImg != null)
			{
				var className = flagImg.ClassName ?? "";
				var flagMatch = System.Text.RegularExpressions.Regex.Match(className, @"icon-flag-(\w+)");
				if (flagMatch.Success)
				{
					var code = flagMatch.Groups[1].Value.ToLowerInvariant();
					var mapped = CountryFromCode(code);
					if (mapped != null)
						return mapped;
				}
			}
		}

		// Try mobile h3: "Bad Gastein, Österrike"
		var h3 = container.QuerySelector("h3");
		if (h3 != null)
		{
			var text = h3.TextContent?.Trim() ?? "";
			var commaIdx = text.LastIndexOf(',');
			if (commaIdx >= 0)
				return text[(commaIdx + 1)..].Trim();
		}

		return "Okänt";
	}

	internal static string? CountryFromCode(string code) =>
		code switch
		{
			"at" => "Österrike",
			"it" => "Italien",
			"fr" => "Frankrike",
			"ch" => "Schweiz",
			"no" => "Norge",
			"se" => "Sverige",
			"de" => "Tyskland",
			"ad" => "Andorra",
			"bg" => "Bulgarien",
			"es" => "Spanien",
			"fi" => "Finland",
			_ => null,
		};

	internal static bool TryParsePrice(string? text, out int price)
	{
		price = 0;
		if (string.IsNullOrEmpty(text))
			return false;

		// Remove dots (thousands separator), ":-" suffix, and any non-digit chars
		var digits = new string(text.Where(char.IsDigit).ToArray());
		return int.TryParse(digits, out price) && price > 0;
	}

	internal static DateOnly? ParseDate(string? text)
	{
		if (string.IsNullOrEmpty(text))
			return null;

		// Format: DD.MM.YYYY
		if (DateOnly.TryParseExact(text, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
			return date;

		return null;
	}

	private static string ExtractExternalId(string bookingUrl, string destination, int price)
	{
		if (!string.IsNullOrEmpty(bookingUrl))
		{
			// Try to get id param from URL
			try
			{
				var uri = new Uri(bookingUrl);
				var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
				var id = query["id"];
				if (!string.IsNullOrEmpty(id))
					return $"slopestar-{id}";
			}
			catch
			{
				// Fall through
			}
		}

		return $"slopestar-{(destination + price).GetHashCode():x8}";
	}
}
