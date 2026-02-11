using AngleSharp;
using AngleSharp.Dom;
using Skidjakt.Scraper.Scrapers;

namespace Skidjakt.Tests.Scrapers;

public class SlopestarScraperTests
{
	private static async Task<IDocument> LoadFixture(string filename)
	{
		var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", filename);
		var html = await File.ReadAllTextAsync(fixturePath);
		var config = Configuration.Default;
		var context = BrowsingContext.New(config);
		return await context.OpenAsync(req => req.Content(html));
	}

	[Fact]
	public async Task ParseDocument_ReturnsCorrectDealCount()
	{
		var document = await LoadFixture("slopestar-ajax-page1.html");
		var deals = SlopestarScraper.ParseDocument(document);

		Assert.Equal(3, deals.Count);
	}

	[Fact]
	public async Task ParseDocument_ParsesDealWithAllFields()
	{
		var document = await LoadFixture("slopestar-ajax-page1.html");
		var deals = SlopestarScraper.ParseDocument(document);

		var badGastein = deals.First(d => d.Destination == "Bad Gastein");

		Assert.Equal("slopestar", badGastein.Agency);
		Assert.Equal("Österrike", badGastein.Country);
		Assert.Equal(7845, badGastein.PricePerPerson);
		Assert.Equal(13395, badGastein.OriginalPrice);
		Assert.Equal(13395 - 7845, badGastein.DiscountAmount);
		Assert.Equal(new DateOnly(2025, 3, 22), badGastein.DepartureDate);
		Assert.Equal(7, badGastein.DurationNights);
		Assert.Equal("Hotel Mozart", badGastein.AccommodationName);
		Assert.Equal("Buss", badGastein.TransportType);
		Assert.False(badGastein.IncludesFlight);
		Assert.True(badGastein.IncludesLiftPass);
		Assert.True(badGastein.IncludesTransfer);
	}

	[Fact]
	public async Task ParseDocument_ExtractsCountryFromFlagIcon()
	{
		var document = await LoadFixture("slopestar-ajax-page1.html");
		var deals = SlopestarScraper.ParseDocument(document);

		Assert.Equal("Österrike", deals[0].Country); // icon-flag-at
		Assert.Equal("Italien", deals[1].Country); // icon-flag-it
		Assert.Equal("Frankrike", deals[2].Country); // icon-flag-fr
	}

	[Fact]
	public async Task ParseDocument_HandlesDotThousandsSeparatorInPrice()
	{
		var document = await LoadFixture("slopestar-ajax-page1.html");
		var deals = SlopestarScraper.ParseDocument(document);

		Assert.Equal(7845, deals[0].PricePerPerson); // "7.845"
		Assert.Equal(4295, deals[1].PricePerPerson); // "4.295"
		Assert.Equal(11490, deals[2].PricePerPerson); // "11.490"
	}

	[Fact]
	public async Task ParseDocument_ParsesOriginalPriceAndDiscount()
	{
		var document = await LoadFixture("slopestar-ajax-page1.html");
		var deals = SlopestarScraper.ParseDocument(document);

		// Bad Gastein has original price
		Assert.Equal(13395, deals[0].OriginalPrice);
		Assert.Equal(13395 - 7845, deals[0].DiscountAmount);

		// Passo Tonale has no original price
		Assert.Null(deals[1].OriginalPrice);
		Assert.Null(deals[1].DiscountAmount);

		// Les Deux Alpes has original price
		Assert.Equal(15990, deals[2].OriginalPrice);
	}

	[Fact]
	public async Task ParseDocument_DetectsTransportTypes()
	{
		var document = await LoadFixture("slopestar-ajax-page1.html");
		var deals = SlopestarScraper.ParseDocument(document);

		Assert.Equal("Buss", deals[0].TransportType);
		Assert.Equal("Kör själv", deals[1].TransportType);
		Assert.Equal("Flyg", deals[2].TransportType);
	}

	[Fact]
	public async Task ParseDocument_DetectsFlightInclusion()
	{
		var document = await LoadFixture("slopestar-ajax-page1.html");
		var deals = SlopestarScraper.ParseDocument(document);

		Assert.False(deals[0].IncludesFlight); // Buss
		Assert.False(deals[1].IncludesFlight); // Kör själv
		Assert.True(deals[2].IncludesFlight); // Flyg
	}

	[Fact]
	public async Task ParseDocument_DetectsLiftPassFromPriceIncludes()
	{
		var document = await LoadFixture("slopestar-ajax-page1.html");
		var deals = SlopestarScraper.ParseDocument(document);

		Assert.True(deals[0].IncludesLiftPass); // "Inkluderar: liftkort, transfer"
		Assert.False(deals[1].IncludesLiftPass); // No price-includes
		Assert.False(deals[2].IncludesLiftPass); // "Inkluderar: halvpension"
	}

	[Fact]
	public async Task ParseDocument_DetectsMealInclusion()
	{
		var document = await LoadFixture("slopestar-ajax-page1.html");
		var deals = SlopestarScraper.ParseDocument(document);

		Assert.False(deals[0].IncludesMeals);
		Assert.False(deals[1].IncludesMeals);
		Assert.True(deals[2].IncludesMeals); // "Inkluderar: halvpension"
		Assert.Equal("Halvpension", deals[2].MealDescription);
	}

	[Fact]
	public async Task ParseDocument_ParsesDuration()
	{
		var document = await LoadFixture("slopestar-ajax-page1.html");
		var deals = SlopestarScraper.ParseDocument(document);

		Assert.Equal(7, deals[0].DurationNights);
		Assert.Equal(4, deals[1].DurationNights);
		Assert.Equal(7, deals[2].DurationNights);
	}

	[Fact]
	public async Task ParseDocument_SetsAgencyForAllDeals()
	{
		var document = await LoadFixture("slopestar-ajax-page1.html");
		var deals = SlopestarScraper.ParseDocument(document);

		Assert.All(deals, d => Assert.Equal("slopestar", d.Agency));
	}

	[Fact]
	public async Task ParseDocument_ExtractsBookingUrl()
	{
		var document = await LoadFixture("slopestar-ajax-page1.html");
		var deals = SlopestarScraper.ParseDocument(document);

		Assert.Contains("id=SS-1001", deals[0].SourceUrl);
		Assert.Contains("id=SS-1002", deals[1].SourceUrl);
		Assert.Contains("id=SS-1003", deals[2].SourceUrl);
	}

	[Fact]
	public async Task ParseDocument_ExtractsExternalIdFromBookingUrl()
	{
		var document = await LoadFixture("slopestar-ajax-page1.html");
		var deals = SlopestarScraper.ParseDocument(document);

		Assert.Equal("slopestar-SS-1001", deals[0].ExternalId);
		Assert.Equal("slopestar-SS-1002", deals[1].ExternalId);
		Assert.Equal("slopestar-SS-1003", deals[2].ExternalId);
	}

	[Theory]
	[InlineData("at", "Österrike")]
	[InlineData("it", "Italien")]
	[InlineData("fr", "Frankrike")]
	[InlineData("ch", "Schweiz")]
	[InlineData("no", "Norge")]
	[InlineData("xx", null)]
	public void CountryFromCode_ReturnsCorrectMapping(string code, string? expected)
	{
		Assert.Equal(expected, SlopestarScraper.CountryFromCode(code));
	}

	[Theory]
	[InlineData("22.03.2025", 2025, 3, 22)]
	[InlineData("15.03.2025", 2025, 3, 15)]
	public void ParseDate_ParsesDotFormat(string input, int year, int month, int day)
	{
		var result = SlopestarScraper.ParseDate(input);
		Assert.NotNull(result);
		Assert.Equal(new DateOnly(year, month, day), result);
	}

	[Theory]
	[InlineData("")]
	[InlineData(null)]
	[InlineData("invalid")]
	public void ParseDate_ReturnsNullForInvalidInput(string? input)
	{
		Assert.Null(SlopestarScraper.ParseDate(input));
	}

	[Fact]
	public async Task ParseDocument_ExtractsRoomType()
	{
		var document = await LoadFixture("slopestar-ajax-page1.html");
		var deals = SlopestarScraper.ParseDocument(document);

		Assert.Equal("2-bäddsrum à 2 pers.", deals[0].RoomType);
		Assert.Null(deals[1].RoomType); // span title = accommodation name, not room type
		Assert.Equal("3-bäddsrum à 3 pers.", deals[2].RoomType);
	}

	[Fact]
	public async Task ParseDocument_ParsesPersonCount()
	{
		var document = await LoadFixture("slopestar-ajax-page1.html");
		var deals = SlopestarScraper.ParseDocument(document);

		Assert.Equal(2, deals[0].PersonCount);
		Assert.Null(deals[1].PersonCount); // no room type with "pers"
		Assert.Equal(3, deals[2].PersonCount);
	}

	[Fact]
	public async Task ParseDocument_ParsesDistances()
	{
		var document = await LoadFixture("slopestar-ajax-page1.html");
		var deals = SlopestarScraper.ParseDocument(document);

		// Bad Gastein has distances
		Assert.Equal(500, deals[0].DistanceToLiftMeters);
		Assert.Equal(400, deals[0].DistanceToSlopeMeters);
		Assert.Equal(200, deals[0].DistanceToCentreMeters);

		// Passo Tonale has no distances
		Assert.Null(deals[1].DistanceToLiftMeters);
		Assert.Null(deals[1].DistanceToSlopeMeters);
		Assert.Null(deals[1].DistanceToCentreMeters);

		// Les Deux Alpes has distances
		Assert.Equal(100, deals[2].DistanceToLiftMeters);
		Assert.Equal(50, deals[2].DistanceToSlopeMeters);
		Assert.Equal(300, deals[2].DistanceToCentreMeters);
	}

	[Theory]
	[InlineData("2-bäddsrum à 2 pers.", 2)]
	[InlineData("3-bäddsrum à 3 pers.", 3)]
	[InlineData("5-bäddsrum à 5 pers.", 5)]
	[InlineData("Dubbelrum à 2 pers.", 2)]
	[InlineData(null, null)]
	[InlineData("", null)]
	[InlineData("Hotel Mozart", null)]
	public void ParsePersonCount_ExtractsCorrectCount(string? input, int? expected)
	{
		Assert.Equal(expected, SlopestarScraper.ParsePersonCount(input));
	}

	[Fact]
	public void ParseDistances_ExtractsAllDistances()
	{
		SlopestarScraper.ParseDistances("Liften: 500 m / Pisten: 400 m / Centrum: 200 m", out var lift, out var slope, out var centre);

		Assert.Equal(500, lift);
		Assert.Equal(400, slope);
		Assert.Equal(200, centre);
	}

	[Fact]
	public void ParseDistances_HandlesEmptyString()
	{
		SlopestarScraper.ParseDistances("", out var lift, out var slope, out var centre);

		Assert.Null(lift);
		Assert.Null(slope);
		Assert.Null(centre);
	}

	[Fact]
	public void ParseDistances_HandlesPartialDistances()
	{
		SlopestarScraper.ParseDistances("Liften: 300 m", out var lift, out var slope, out var centre);

		Assert.Equal(300, lift);
		Assert.Null(slope);
		Assert.Null(centre);
	}
}
