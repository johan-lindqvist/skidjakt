using AngleSharp;
using AngleSharp.Dom;
using Skidjakt.Scraper.Scrapers;

namespace Skidjakt.Tests.Scrapers;

public class SkilinkScraperTests
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
		var document = await LoadFixture("skilink-page1.html");
		var deals = SkilinkScraper.ParseDocument(document);

		// 4 item-rows in fixture (skips row-daybreak and iteminfo-row)
		Assert.Equal(4, deals.Count);
	}

	[Fact]
	public async Task ParseDocument_ParsesStandardRowWithMetadata()
	{
		var document = await LoadFixture("skilink-page1.html");
		var deals = SkilinkScraper.ParseDocument(document);

		var livigno = deals.First(d => d.Destination == "Livigno");

		Assert.Equal("skilink", livigno.Agency);
		Assert.Equal("Italien", livigno.Country);
		Assert.Equal(5099, livigno.PricePerPerson);
		Assert.Equal(new DateOnly(2025, 3, 15), livigno.DepartureDate);
		Assert.Equal("Buss", livigno.TransportType);
		Assert.False(livigno.IncludesFlight);
		Assert.Equal("skilink-12345", livigno.ExternalId);
		Assert.Contains("/boka/12345/", livigno.SourceUrl);
	}

	[Fact]
	public async Task ParseDocument_ParsesFlightDeal()
	{
		var document = await LoadFixture("skilink-page1.html");
		var deals = SkilinkScraper.ParseDocument(document);

		var stAnton = deals.First(d => d.Destination == "St. Anton");

		Assert.Equal("Österrike", stAnton.Country);
		Assert.Equal(12490, stAnton.PricePerPerson);
		Assert.Equal("Flyg - Arlanda", stAnton.TransportType);
		Assert.True(stAnton.IncludesFlight);
	}

	[Fact]
	public async Task ParseDocument_ParsesRowWithoutMetadata()
	{
		var document = await LoadFixture("skilink-page1.html");
		var deals = SkilinkScraper.ParseDocument(document);

		var chamonix = deals.First(d => d.Destination == "Chamonix");

		Assert.Equal("Frankrike", chamonix.Country);
		Assert.Equal(3200, chamonix.PricePerPerson);
		Assert.Equal("Egen Transport", chamonix.TransportType);
		Assert.False(chamonix.IncludesFlight);
	}

	[Fact]
	public async Task ParseDocument_ExtractsCountryFromPath()
	{
		var document = await LoadFixture("skilink-page1.html");
		var deals = SkilinkScraper.ParseDocument(document);

		Assert.Equal("Italien", deals[0].Country);
		Assert.Equal("Österrike", deals[1].Country);
		Assert.Equal("Frankrike", deals[2].Country);
		Assert.Equal("Norge", deals[3].Country);
	}

	[Theory]
	[InlineData("/skidresor/italien/livigno/", "Italien")]
	[InlineData("/skidresor/osterrike/st-anton/", "Österrike")]
	[InlineData("/skidresor/frankrike/chamonix/", "Frankrike")]
	[InlineData("/skidresor/norge/hemsedal/", "Norge")]
	[InlineData("", "Okänt")]
	[InlineData("/other/path/", "Okänt")]
	public void ExtractCountryFromPath_ReturnsCorrectCountry(string path, string expected)
	{
		Assert.Equal(expected, SkilinkScraper.ExtractCountryFromPath(path));
	}

	[Fact]
	public async Task ParseDocument_HandlesNonBreakingSpaceInPrice()
	{
		// The fixture uses &nbsp; in prices like "5&nbsp;099:-"
		var document = await LoadFixture("skilink-page1.html");
		var deals = SkilinkScraper.ParseDocument(document);

		// All prices should be parsed correctly despite non-breaking spaces
		Assert.All(deals, d => Assert.True(d.PricePerPerson > 0));
	}

	[Fact]
	public async Task ParseDocument_SetsAgencyForAllDeals()
	{
		var document = await LoadFixture("skilink-page1.html");
		var deals = SkilinkScraper.ParseDocument(document);

		Assert.All(deals, d => Assert.Equal("skilink", d.Agency));
	}

	[Fact]
	public async Task ParseDocument_AllDealsHaveExternalId()
	{
		var document = await LoadFixture("skilink-page1.html");
		var deals = SkilinkScraper.ParseDocument(document);

		Assert.All(deals, d => Assert.False(string.IsNullOrEmpty(d.ExternalId)));
	}
}
