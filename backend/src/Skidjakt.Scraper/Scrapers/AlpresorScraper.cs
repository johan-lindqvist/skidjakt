using Microsoft.Extensions.Logging;
using Skidjakt.Core.Entities;
using Skidjakt.Core.Interfaces;

namespace Skidjakt.Scraper.Scrapers;

public class AlpresorScraper : IDealScraper
{
	private readonly ILogger<AlpresorScraper> _logger;

	public string Agency => "alpresor";

	public AlpresorScraper(IHttpClientFactory httpClientFactory, ILogger<AlpresorScraper> logger)
	{
		_logger = logger;
	}

	public Task<IReadOnlyList<Deal>> ScrapeDealsAsync(CancellationToken ct)
	{
		_logger.LogWarning("Alpresor scraper disabled: site requires JavaScript rendering (Playwright needed)");
		return Task.FromResult<IReadOnlyList<Deal>>([]);
	}
}
