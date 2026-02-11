using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Skidjakt.Core.DTOs;
using Skidjakt.Core.Entities;
using Skidjakt.Infrastructure.Data;

namespace Skidjakt.Tests.Infrastructure;

public class DealRepositoryTests : IDisposable
{
	private readonly SqliteConnection _connection;
	private readonly DbContextOptions<SkidjaktDbContext> _options;

	public DealRepositoryTests()
	{
		_connection = new SqliteConnection("DataSource=:memory:");
		_connection.Open();
		_options = new DbContextOptionsBuilder<SkidjaktDbContext>().UseSqlite(_connection).Options;

		using var context = new SkidjaktDbContext(_options);
		context.Database.EnsureCreated();
	}

	public void Dispose()
	{
		_connection.Dispose();
	}

	private SkidjaktDbContext CreateContext() => new(_options);

	private async Task SeedDealsAsync()
	{
		using var context = CreateContext();
		context.Deals.AddRange(
			new Deal
			{
				Agency = "skilink",
				ExternalId = "sk1",
				SourceUrl = "https://skilink.se/deal1",
				Destination = "Livigno",
				Country = "Italien",
				PricePerPerson = 5990,
				OriginalPrice = 7990,
				DiscountAmount = 2000,
				DurationNights = 7,
				DepartureDate = new DateOnly(2026, 3, 1),
				WeekNumber = 10,
				TransportType = "Flyg",
				IncludesFlight = true,
				IncludesLiftPass = true,
				FirstSeen = DateTime.UtcNow,
				LastSeen = DateTime.UtcNow,
				IsActive = true,
			},
			new Deal
			{
				Agency = "alpresor",
				ExternalId = "al1",
				SourceUrl = "https://alpresor.se/deal1",
				Destination = "Val Thorens",
				Country = "Frankrike",
				PricePerPerson = 8990,
				DurationNights = 7,
				DepartureDate = new DateOnly(2026, 3, 8),
				WeekNumber = 11,
				TransportType = "Flyg",
				IncludesFlight = true,
				IncludesMeals = true,
				MealDescription = "Halvpension",
				FirstSeen = DateTime.UtcNow,
				LastSeen = DateTime.UtcNow,
				IsActive = true,
			},
			new Deal
			{
				Agency = "skilink",
				ExternalId = "sk2",
				SourceUrl = "https://skilink.se/deal2",
				Destination = "St. Anton",
				Country = "Österrike",
				PricePerPerson = 4990,
				DurationNights = 5,
				DepartureDate = new DateOnly(2026, 2, 22),
				WeekNumber = 9,
				TransportType = "Buss",
				IncludesLiftPass = true,
				FirstSeen = DateTime.UtcNow,
				LastSeen = DateTime.UtcNow,
				IsActive = true,
			},
			new Deal
			{
				Agency = "nortlander",
				ExternalId = "no1",
				SourceUrl = "https://nortlander.se/deal1",
				Destination = "Åre",
				Country = "Sverige",
				PricePerPerson = 3490,
				DurationNights = 4,
				TransportType = "Egen bil",
				FirstSeen = DateTime.UtcNow,
				LastSeen = DateTime.UtcNow,
				IsActive = false, // inactive deal
			}
		);
		await context.SaveChangesAsync();
	}

	[Fact]
	public async Task GetDealsAsync_ReturnsOnlyActiveDeals()
	{
		await SeedDealsAsync();
		using var context = CreateContext();
		var repo = new DealRepository(context);

		var result = await repo.GetDealsAsync(new DealQuery());

		Assert.Equal(3, result.TotalCount);
		Assert.All(result.Items, d => Assert.True(d.IsActive));
	}

	[Fact]
	public async Task GetDealsAsync_FiltersByAgency()
	{
		await SeedDealsAsync();
		using var context = CreateContext();
		var repo = new DealRepository(context);

		var result = await repo.GetDealsAsync(new DealQuery { Agencies = ["skilink"] });

		Assert.Equal(2, result.TotalCount);
		Assert.All(result.Items, d => Assert.Equal("skilink", d.Agency));
	}

	[Fact]
	public async Task GetDealsAsync_FiltersByCountry()
	{
		await SeedDealsAsync();
		using var context = CreateContext();
		var repo = new DealRepository(context);

		var result = await repo.GetDealsAsync(new DealQuery { Countries = ["Italien"] });

		Assert.Single(result.Items);
		Assert.Equal("Livigno", result.Items[0].Destination);
	}

	[Fact]
	public async Task GetDealsAsync_FiltersByPriceRange()
	{
		await SeedDealsAsync();
		using var context = CreateContext();
		var repo = new DealRepository(context);

		var result = await repo.GetDealsAsync(new DealQuery { MinPrice = 5000, MaxPrice = 7000 });

		Assert.Single(result.Items);
		Assert.Equal("Livigno", result.Items[0].Destination);
	}

	[Fact]
	public async Task GetDealsAsync_SearchByDestination()
	{
		await SeedDealsAsync();
		using var context = CreateContext();
		var repo = new DealRepository(context);

		var result = await repo.GetDealsAsync(new DealQuery { Search = "Livigno" });

		Assert.Single(result.Items);
		Assert.Equal("Livigno", result.Items[0].Destination);
	}

	[Fact]
	public async Task GetDealsAsync_SortsByPriceAscending()
	{
		await SeedDealsAsync();
		using var context = CreateContext();
		var repo = new DealRepository(context);

		var result = await repo.GetDealsAsync(new DealQuery { SortBy = "price" });

		Assert.Equal(4990, result.Items[0].PricePerPerson);
		Assert.Equal(5990, result.Items[1].PricePerPerson);
		Assert.Equal(8990, result.Items[2].PricePerPerson);
	}

	[Fact]
	public async Task GetDealsAsync_SortsByPriceDescending()
	{
		await SeedDealsAsync();
		using var context = CreateContext();
		var repo = new DealRepository(context);

		var result = await repo.GetDealsAsync(new DealQuery { SortBy = "price_desc" });

		Assert.Equal(8990, result.Items[0].PricePerPerson);
		Assert.Equal(5990, result.Items[1].PricePerPerson);
		Assert.Equal(4990, result.Items[2].PricePerPerson);
	}

	[Fact]
	public async Task GetDealsAsync_PaginatesResults()
	{
		await SeedDealsAsync();
		using var context = CreateContext();
		var repo = new DealRepository(context);

		var result = await repo.GetDealsAsync(new DealQuery { Page = 1, PageSize = 2 });

		Assert.Equal(3, result.TotalCount);
		Assert.Equal(2, result.Items.Count);
		Assert.Equal(2, result.TotalPages);
	}

	[Fact]
	public async Task GetDealByIdAsync_ReturnsDealWhenFound()
	{
		await SeedDealsAsync();
		using var context = CreateContext();
		var repo = new DealRepository(context);

		var deal = await repo.GetDealByIdAsync(1);

		Assert.NotNull(deal);
		Assert.Equal("skilink", deal.Agency);
	}

	[Fact]
	public async Task GetDealByIdAsync_ReturnsNullWhenNotFound()
	{
		await SeedDealsAsync();
		using var context = CreateContext();
		var repo = new DealRepository(context);

		var deal = await repo.GetDealByIdAsync(999);

		Assert.Null(deal);
	}

	[Fact]
	public async Task GetFilterOptionsAsync_ReturnsDistinctValues()
	{
		await SeedDealsAsync();
		using var context = CreateContext();
		var repo = new DealRepository(context);

		var options = await repo.GetFilterOptionsAsync();

		Assert.Contains("skilink", options.Agencies);
		Assert.Contains("alpresor", options.Agencies);
		Assert.DoesNotContain("nortlander", options.Agencies); // inactive deal
		Assert.Contains("Italien", options.Countries);
		Assert.Contains("Frankrike", options.Countries);
	}

	[Fact]
	public async Task GetStatsAsync_ReturnsCorrectStats()
	{
		await SeedDealsAsync();
		using var context = CreateContext();
		var repo = new DealRepository(context);

		var stats = await repo.GetStatsAsync();

		Assert.Equal(3, stats.TotalDeals);
		Assert.Equal(4990, stats.LowestPrice);
		Assert.True(stats.DealsPerAgency.ContainsKey("skilink"));
		Assert.Equal(2, stats.DealsPerAgency["skilink"]);
	}

	[Fact]
	public async Task UpsertDealsAsync_InsertsNewDeals()
	{
		using var context = CreateContext();
		var repo = new DealRepository(context);

		var newDeals = new List<Deal>
		{
			new()
			{
				Agency = "skilink",
				ExternalId = "new1",
				SourceUrl = "https://skilink.se/new1",
				Destination = "Zermatt",
				Country = "Schweiz",
				PricePerPerson = 12990,
				DurationNights = 7,
			},
		};

		await repo.UpsertDealsAsync("skilink", newDeals);

		using var verifyContext = CreateContext();
		var deal = await verifyContext.Deals.FirstOrDefaultAsync(d => d.ExternalId == "new1");
		Assert.NotNull(deal);
		Assert.Equal("Zermatt", deal.Destination);
		Assert.True(deal.IsActive);
	}

	[Fact]
	public async Task UpsertDealsAsync_UpdatesExistingDeals()
	{
		await SeedDealsAsync();
		using var context = CreateContext();
		var repo = new DealRepository(context);

		var updatedDeals = new List<Deal>
		{
			new()
			{
				Agency = "skilink",
				ExternalId = "sk1",
				SourceUrl = "https://skilink.se/deal1",
				Destination = "Livigno",
				Country = "Italien",
				PricePerPerson = 4990, // price changed
				DurationNights = 7,
			},
		};

		await repo.UpsertDealsAsync("skilink", updatedDeals);

		using var verifyContext = CreateContext();
		var deal = await verifyContext.Deals.FirstAsync(d => d.ExternalId == "sk1");
		Assert.Equal(4990, deal.PricePerPerson);
	}

	[Fact]
	public async Task UpsertDealsAsync_MarksMissingDealsAsInactive()
	{
		await SeedDealsAsync();
		using var context = CreateContext();
		var repo = new DealRepository(context);

		// Only sk1 is in the new scrape; sk2 should become inactive
		var deals = new List<Deal>
		{
			new()
			{
				Agency = "skilink",
				ExternalId = "sk1",
				SourceUrl = "https://skilink.se/deal1",
				Destination = "Livigno",
				Country = "Italien",
				PricePerPerson = 5990,
				DurationNights = 7,
			},
		};

		await repo.UpsertDealsAsync("skilink", deals);

		using var verifyContext = CreateContext();
		var sk2 = await verifyContext.Deals.FirstAsync(d => d.ExternalId == "sk2");
		Assert.False(sk2.IsActive);
	}
}
