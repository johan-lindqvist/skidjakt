using System.Globalization;
using System.Text;
using AngleSharp;
using AngleSharp.Dom;
using Microsoft.Extensions.Logging;
using Skidjakt.Core.Entities;
using Skidjakt.Core.Interfaces;

namespace Skidjakt.Scraper.Scrapers;

public class SkilinkScraper : IDealScraper
{
	private const string BaseUrl = "https://www.skilink.se";
	private const string ListUrl = $"{BaseUrl}/sista-minuten/";
	private const int MaxPages = 10;

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
		var allDeals = new List<Deal>();

		try
		{
			var client = _httpClientFactory.CreateClient("Skilink");
			var config = Configuration.Default;
			var context = BrowsingContext.New(config);

			for (int page = 1; page <= MaxPages; page++)
			{
				ct.ThrowIfCancellationRequested();

				var url = page == 1 ? ListUrl : $"{ListUrl}?pagenumber={page}";
				var response = await client.GetAsync(url, ct);
				var bytes = await response.Content.ReadAsByteArrayAsync(ct);
				var charset = response.Content.Headers.ContentType?.CharSet;
				var encoding = !string.IsNullOrEmpty(charset) ? Encoding.GetEncoding(charset) : Encoding.Latin1;
				var html = encoding.GetString(bytes);
				var document = await context.OpenAsync(req => req.Content(html), ct);

				var deals = ParseDocument(document);
				if (deals.Count == 0)
					break;

				allDeals.AddRange(deals);
				_logger.LogDebug("Skilink page {Page}: {Count} deals", page, deals.Count);

				// Check if there are more pages
				var totalPages = GetTotalPages(document);
				if (page >= totalPages)
					break;

				// Be polite
				await Task.Delay(500, ct);
			}

			// Deduplicate by ExternalId (pages may have overlapping deals)
			var deduplicated = allDeals.GroupBy(d => d.ExternalId).Select(g => g.Last()).ToList();

			_logger.LogInformation(
				"Scraped {Count} deals from Skilink ({Raw} raw, {Dupes} duplicates removed)",
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
			_logger.LogError(ex, "Failed to scrape Skilink");
		}

		return allDeals;
	}

	internal static List<Deal> ParseDocument(IDocument document)
	{
		var deals = new List<Deal>();
		var rows = document.QuerySelectorAll("table#tourlist-table tr.item-row");

		foreach (var row in rows)
		{
			var deal = ParseRow(row);
			if (deal != null)
				deals.Add(deal);
		}

		return deals;
	}

	internal static Deal? ParseRow(IElement row)
	{
		var cells = row.QuerySelectorAll("td");
		if (cells.Length < 5)
			return null;

		var dateCell = cells[0];
		var transportCell = cells[1];
		var destinationCell = cells[2];
		// cells[3] is availability
		var priceCell = cells[4];
		var buttonCell = cells.Length > 5 ? cells[5] : null;

		// Destination
		var destinationLink = destinationCell.QuerySelector("a.subject");
		var destination = destinationLink?.QuerySelector("span")?.TextContent?.Trim() ?? destinationLink?.TextContent?.Trim();
		if (string.IsNullOrEmpty(destination))
			return null;

		// Price - prefer meta itemprop, fallback to span.theprice
		if (!TryParsePrice(priceCell, out var price))
			return null;

		// Booking URL
		var bookingLink = buttonCell?.QuerySelector("a.button")?.GetAttribute("href") ?? "";
		if (!string.IsNullOrEmpty(bookingLink) && !bookingLink.StartsWith("http"))
			bookingLink = BaseUrl + bookingLink;

		// Destination URL (for country extraction)
		var destHref = destinationLink?.GetAttribute("href") ?? "";

		// Country from URL path: /skidresor/italien/livigno/ → Italien
		var country = ExtractCountryFromPath(destHref);

		// Date - prefer meta itemprop, fallback to text
		var departureDate = ParseDate(dateCell);

		// Transport
		var transport = transportCell.TextContent?.Trim();

		// External ID from booking link or destination+date hash
		var externalId = !string.IsNullOrEmpty(bookingLink)
			? ExtractExternalId(bookingLink)
			: $"skilink-{destination}-{departureDate}-{price}".GetHashCode().ToString("x8");

		// Source URL (destination page or booking page)
		var sourceUrl =
			!string.IsNullOrEmpty(bookingLink) ? bookingLink
			: !string.IsNullOrEmpty(destHref) && !destHref.StartsWith("http") ? BaseUrl + destHref
			: ListUrl;

		var includesFlight = transport?.Contains("Flyg", StringComparison.OrdinalIgnoreCase) ?? false;

		return new Deal
		{
			Agency = "skilink",
			ExternalId = externalId,
			SourceUrl = sourceUrl,
			Destination = destination,
			Country = country,
			PricePerPerson = price,
			DurationNights = 7,
			DepartureDate = departureDate,
			TransportType = transport,
			IncludesFlight = includesFlight,
		};
	}

	internal static bool TryParsePrice(IElement priceCell, out int price)
	{
		price = 0;

		// Try meta itemprop="price" first
		var metaPrice = priceCell.QuerySelector("meta[itemprop='price']");
		if (metaPrice != null)
		{
			var content = metaPrice.GetAttribute("content");
			if (int.TryParse(content, out price) && price > 0)
				return true;
		}

		// Fallback to span.theprice text: "5 099:-" or "5\u00a0099:-"
		var priceText = priceCell.QuerySelector("span.theprice")?.TextContent;
		if (string.IsNullOrEmpty(priceText))
			return false;

		// Remove non-breaking spaces, regular spaces, and ":-" suffix
		var digits = new string(priceText.Where(char.IsDigit).ToArray());
		return int.TryParse(digits, out price) && price > 0;
	}

	internal static string ExtractCountryFromPath(string path)
	{
		if (string.IsNullOrEmpty(path))
			return "Okänt";

		// Path: /skidresor/italien/livigno/ → segments[2] = "italien"
		var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
		if (segments.Length >= 2 && segments[0] == "skidresor")
		{
			var countrySlug = segments[1].ToLowerInvariant();
			return CountryFromSlug(countrySlug);
		}

		return "Okänt";
	}

	private static string CountryFromSlug(string slug) =>
		slug switch
		{
			"italien" => "Italien",
			"osterrike" or "österrike" => "Österrike",
			"frankrike" => "Frankrike",
			"norge" => "Norge",
			"sverige" => "Sverige",
			"schweiz" => "Schweiz",
			"andorra" => "Andorra",
			"bulgarien" => "Bulgarien",
			"spanien" => "Spanien",
			"finland" => "Finland",
			"tyskland" => "Tyskland",
			_ => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(slug),
		};

	internal static DateOnly? ParseDate(IElement dateCell)
	{
		// Try meta itemprop="startDate" first
		var metaDate = dateCell.QuerySelector("meta[itemprop='startDate']");
		if (metaDate != null)
		{
			var content = metaDate.GetAttribute("content");
			if (DateOnly.TryParse(content, out var metaParsed))
				return metaParsed;
		}

		// Fallback: parse "DD/M" text, assume current or next season year
		var text = dateCell.TextContent?.Trim();
		if (string.IsNullOrEmpty(text))
			return null;

		// Extract DD/M pattern
		var parts = text.Split('/');
		if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out var day) && int.TryParse(parts[1].Trim(), out var month))
		{
			var year = GuessYear(month);
			try
			{
				return new DateOnly(year, month, day);
			}
			catch
			{
				return null;
			}
		}

		return null;
	}

	private static int GuessYear(int month)
	{
		var now = DateTime.Now;
		// Ski season: Oct-Apr. If month is Oct-Dec, use current year.
		// If month is Jan-Apr, could be current or next year.
		if (month >= 10)
			return now.Year;
		// If we're past April, assume next year's season
		if (now.Month > 4)
			return now.Year + 1;
		return now.Year;
	}

	private static int GetTotalPages(IDocument document)
	{
		var pagerText = document.QuerySelector(".pager")?.TextContent ?? "";
		// "Sida 1 av 43" → extract 43
		var match = System.Text.RegularExpressions.Regex.Match(pagerText, @"av\s+(\d+)");
		if (match.Success && int.TryParse(match.Groups[1].Value, out var total))
			return Math.Min(total, MaxPages);
		return 1;
	}

	private static string ExtractExternalId(string url)
	{
		// /boka/12345/ → "skilink-12345"
		var segments = url.Split('/', StringSplitOptions.RemoveEmptyEntries);
		var bokaIndex = Array.IndexOf(segments, "boka");
		if (bokaIndex >= 0 && bokaIndex + 1 < segments.Length)
			return $"skilink-{segments[bokaIndex + 1]}";
		return url.GetHashCode().ToString("x8");
	}
}
