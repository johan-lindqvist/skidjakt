using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Skidjakt.Core.Interfaces;

namespace Skidjakt.Scraper.Services;

public class ScrapingOptions
{
    public int IntervalMinutes { get; set; } = 30;
}

public class ScrapingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScrapingBackgroundService> _logger;
    private readonly ScrapingOptions _options;
    private readonly ScrapeEventService _eventService;

    public ScrapingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ScrapingBackgroundService> logger,
        IOptions<ScrapingOptions> options,
        ScrapeEventService eventService
    )
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
        _eventService = eventService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Scraping service started, interval: {Interval} minutes",
            _options.IntervalMinutes
        );

        // Run initial scrape after a short delay
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        await RunAllScrapersAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_options.IntervalMinutes));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunAllScrapersAsync(stoppingToken);
        }
    }

    public async Task RunAllScrapersAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting scrape run");

        using var scope = _scopeFactory.CreateScope();
        var scrapers = scope.ServiceProvider.GetRequiredService<IEnumerable<IDealScraper>>();
        var repository = scope.ServiceProvider.GetRequiredService<IDealRepository>();

        foreach (var scraper in scrapers)
        {
            try
            {
                _logger.LogInformation("Scraping {Agency}...", scraper.Agency);
                var deals = await scraper.ScrapeDealsAsync(ct);
                _logger.LogInformation(
                    "Found {Count} deals from {Agency}",
                    deals.Count,
                    scraper.Agency
                );

                await repository.UpsertDealsAsync(scraper.Agency, deals, ct);
                _eventService.NotifyDealsUpdated(scraper.Agency, deals.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to scrape {Agency}", scraper.Agency);
            }
        }

        _logger.LogInformation("Scrape run complete");
    }
}
