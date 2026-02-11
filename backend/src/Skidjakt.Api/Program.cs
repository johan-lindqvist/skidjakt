using Microsoft.EntityFrameworkCore;
using Serilog;
using Skidjakt.Core.DTOs;
using Skidjakt.Core.Interfaces;
using Skidjakt.Infrastructure;
using Skidjakt.Infrastructure.Data;
using Skidjakt.Scraper;
using Skidjakt.Scraper.Services;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, config) => config.ReadFrom.Configuration(context.Configuration).WriteTo.Console().Enrich.FromLogContext());

// Services
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScraper(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<SkidjaktDbContext>();
	await db.Database.EnsureCreatedAsync();
}

app.UseSerilogRequestLogging();
app.UseCors();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

// API Endpoints
var api = app.MapGroup("/api");

// GET /api/deals - List deals with filtering
api.MapGet(
	"/deals",
	async (
		IDealRepository repo,
		string? search,
		string? agencies,
		string? countries,
		string? destinations,
		int? minPrice,
		int? maxPrice,
		DateOnly? fromDate,
		DateOnly? toDate,
		string? transportTypes,
		string? sortBy,
		int? page,
		int? pageSize,
		CancellationToken ct
	) =>
	{
		var query = new DealQuery
		{
			Search = search,
			Agencies = agencies?.Split(',', StringSplitOptions.RemoveEmptyEntries),
			Countries = countries?.Split(',', StringSplitOptions.RemoveEmptyEntries),
			Destinations = destinations?.Split(',', StringSplitOptions.RemoveEmptyEntries),
			MinPrice = minPrice,
			MaxPrice = maxPrice,
			FromDate = fromDate,
			ToDate = toDate,
			TransportTypes = transportTypes?.Split(',', StringSplitOptions.RemoveEmptyEntries),
			SortBy = sortBy,
			Page = page ?? 1,
			PageSize = Math.Min(pageSize ?? 30, 100),
		};
		return Results.Ok(await repo.GetDealsAsync(query, ct));
	}
);

// GET /api/deals/{id}
api.MapGet(
	"/deals/{id:int}",
	async (int id, IDealRepository repo, CancellationToken ct) =>
	{
		var deal = await repo.GetDealByIdAsync(id, ct);
		return deal is not null ? Results.Ok(deal) : Results.NotFound();
	}
);

// GET /api/deals/filters
api.MapGet("/deals/filters", async (IDealRepository repo, CancellationToken ct) => Results.Ok(await repo.GetFilterOptionsAsync(ct)));

// GET /api/deals/stats
api.MapGet("/deals/stats", async (IDealRepository repo, CancellationToken ct) => Results.Ok(await repo.GetStatsAsync(ct)));

// GET /api/deals/stream - SSE endpoint
api.MapGet(
	"/deals/stream",
	async (ScrapeEventService eventService, HttpContext context, CancellationToken ct) =>
	{
		context.Response.ContentType = "text/event-stream";
		context.Response.Headers.CacheControl = "no-cache";
		context.Response.Headers.Connection = "keep-alive";

		var writer = new StreamWriter(context.Response.Body);
		await writer.WriteLineAsync("event: connected");
		await writer.WriteLineAsync("data: {\"status\":\"connected\"}");
		await writer.WriteLineAsync();
		await writer.FlushAsync(ct);

		var id = eventService.Subscribe(writer);
		try
		{
			// Keep connection alive until client disconnects
			while (!ct.IsCancellationRequested)
			{
				await Task.Delay(30000, ct);
				await writer.WriteLineAsync(": keepalive");
				await writer.FlushAsync(ct);
			}
		}
		catch (OperationCanceledException) { }
		finally
		{
			eventService.Unsubscribe(id);
		}
	}
);

// GET /api/health
api.MapGet(
	"/health",
	async (IDealRepository repo, CancellationToken ct) =>
	{
		var stats = await repo.GetStatsAsync(ct);
		return Results.Ok(
			new
			{
				status = "healthy",
				totalDeals = stats.TotalDeals,
				lastUpdated = stats.LastUpdated,
				timestamp = DateTime.UtcNow,
			}
		);
	}
);

// POST /api/scrape/trigger
api.MapPost(
	"/scrape/trigger",
	async (IServiceScopeFactory scopeFactory, ScrapeEventService eventService, ILogger<Program> logger, CancellationToken ct) =>
	{
		// Run scrape in background
		_ = Task.Run(
			async () =>
			{
				try
				{
					using var scope = scopeFactory.CreateScope();
					var scrapers = scope.ServiceProvider.GetRequiredService<IEnumerable<IDealScraper>>();
					var repository = scope.ServiceProvider.GetRequiredService<IDealRepository>();

					foreach (var scraper in scrapers)
					{
						try
						{
							var deals = await scraper.ScrapeDealsAsync(ct);
							await repository.UpsertDealsAsync(scraper.Agency, deals, ct);
							eventService.NotifyDealsUpdated(scraper.Agency, deals.Count);
						}
						catch (Exception ex)
						{
							logger.LogError(ex, "Failed to scrape {Agency}", scraper.Agency);
						}
					}
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Scrape trigger failed");
				}
			},
			ct
		);

		return Results.Accepted(value: new { message = "Scrape triggered" });
	}
);

app.Run();
