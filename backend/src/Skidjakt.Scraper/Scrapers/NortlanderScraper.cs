using Microsoft.Extensions.Logging;
using Skidjakt.Core.Entities;
using Skidjakt.Core.Interfaces;

namespace Skidjakt.Scraper.Scrapers;

public class NortlanderScraper : IDealScraper
{
	private readonly ILogger<NortlanderScraper> _logger;

	public string Agency => "nortlander";

	public NortlanderScraper(IHttpClientFactory httpClientFactory, ILogger<NortlanderScraper> logger)
	{
		_logger = logger;
	}

	public Task<IReadOnlyList<Deal>> ScrapeDealsAsync(CancellationToken ct)
	{
		_logger.LogWarning("Nortlander scraper disabled: site requires JavaScript rendering (Playwright needed)");
		return Task.FromResult<IReadOnlyList<Deal>>([]);
	}
}
