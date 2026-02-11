using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Skidjakt.Core.Interfaces;
using Skidjakt.Scraper.Scrapers;
using Skidjakt.Scraper.Services;

namespace Skidjakt.Scraper;

public static class DependencyInjection
{
    public static IServiceCollection AddScraper(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<ScrapingOptions>(configuration.GetSection("Scraping"));
        services.AddSingleton<ScrapeEventService>();
        services.AddHttpClient(
            "Skilink",
            client =>
            {
                client.DefaultRequestHeaders.Add(
                    "User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
                );
                client.Timeout = TimeSpan.FromSeconds(30);
            }
        );
        services.AddHttpClient(
            "Default",
            client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            }
        );
        services.AddScoped<IDealScraper, SkilinkScraper>();
        services.AddScoped<IDealScraper, AlpresorScraper>();
        services.AddScoped<IDealScraper, NortlanderScraper>();
        services.AddScoped<IDealScraper, SlopestarScraper>();
        services.AddHostedService<ScrapingBackgroundService>();
        return services;
    }
}
