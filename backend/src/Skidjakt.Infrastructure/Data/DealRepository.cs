using Microsoft.EntityFrameworkCore;
using Skidjakt.Core.DTOs;
using Skidjakt.Core.Entities;
using Skidjakt.Core.Interfaces;

namespace Skidjakt.Infrastructure.Data;

public class DealRepository : IDealRepository
{
	private readonly SkidjaktDbContext _db;

	public DealRepository(SkidjaktDbContext db)
	{
		_db = db;
	}

	public async Task<PagedResult<DealDto>> GetDealsAsync(DealQuery query, CancellationToken ct = default)
	{
		var q = _db.Deals.AsNoTracking().Where(d => d.IsActive);

		// Text search across Destination, Country, and AccommodationName
		if (!string.IsNullOrWhiteSpace(query.Search))
		{
			var search = query.Search.Trim().ToLower();
			q = q.Where(d =>
				d.Destination.ToLower().Contains(search)
				|| d.Country.ToLower().Contains(search)
				|| (d.AccommodationName != null && d.AccommodationName.ToLower().Contains(search))
			);
		}

		// Filter by agencies
		if (query.Agencies is { Length: > 0 })
		{
			q = q.Where(d => query.Agencies.Contains(d.Agency));
		}

		// Filter by countries
		if (query.Countries is { Length: > 0 })
		{
			q = q.Where(d => query.Countries.Contains(d.Country));
		}

		// Filter by destinations
		if (query.Destinations is { Length: > 0 })
		{
			q = q.Where(d => query.Destinations.Contains(d.Destination));
		}

		// Price range
		if (query.MinPrice.HasValue)
		{
			q = q.Where(d => d.PricePerPerson >= query.MinPrice.Value);
		}

		if (query.MaxPrice.HasValue)
		{
			q = q.Where(d => d.PricePerPerson <= query.MaxPrice.Value);
		}

		// Date range
		if (query.FromDate.HasValue)
		{
			q = q.Where(d => d.DepartureDate >= query.FromDate.Value);
		}

		if (query.ToDate.HasValue)
		{
			q = q.Where(d => d.DepartureDate <= query.ToDate.Value);
		}

		// Transport types
		if (query.TransportTypes is { Length: > 0 })
		{
			q = q.Where(d => d.TransportType != null && query.TransportTypes.Contains(d.TransportType));
		}

		// Sorting
		q = query.SortBy?.ToLower() switch
		{
			"price" => q.OrderBy(d => d.PricePerPerson),
			"price_desc" => q.OrderByDescending(d => d.PricePerPerson),
			"date" => q.OrderBy(d => d.DepartureDate),
			"date_desc" => q.OrderByDescending(d => d.DepartureDate),
			"discount" => q.OrderByDescending(d => d.DiscountAmount ?? 0),
			"newest" => q.OrderByDescending(d => d.FirstSeen),
			_ => q.OrderBy(d => d.PricePerPerson),
		};

		var totalCount = await q.CountAsync(ct);
		var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);

		var items = await q.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).Select(d => MapToDto(d)).ToListAsync(ct);

		return new PagedResult<DealDto>(items, totalCount, query.Page, query.PageSize, totalPages);
	}

	public async Task<DealDto?> GetDealByIdAsync(int id, CancellationToken ct = default)
	{
		var deal = await _db.Deals.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);

		return deal is null ? null : MapToDto(deal);
	}

	public async Task<DealFilterOptions> GetFilterOptionsAsync(CancellationToken ct = default)
	{
		var activeDeals = _db.Deals.AsNoTracking().Where(d => d.IsActive);

		var agencies = await activeDeals.Select(d => d.Agency).Distinct().OrderBy(a => a).ToListAsync(ct);

		var countries = await activeDeals.Select(d => d.Country).Distinct().OrderBy(c => c).ToListAsync(ct);

		var destinations = await activeDeals.Select(d => d.Destination).Distinct().OrderBy(d => d).ToListAsync(ct);

		var transportTypes = await activeDeals
			.Where(d => d.TransportType != null)
			.Select(d => d.TransportType!)
			.Distinct()
			.OrderBy(t => t)
			.ToListAsync(ct);

		var minPrice = await activeDeals.AnyAsync(ct) ? await activeDeals.MinAsync(d => d.PricePerPerson, ct) : 0;

		var maxPrice = await activeDeals.AnyAsync(ct) ? await activeDeals.MaxAsync(d => d.PricePerPerson, ct) : 0;

		return new DealFilterOptions(agencies, countries, destinations, transportTypes, minPrice, maxPrice);
	}

	public async Task<DealStats> GetStatsAsync(CancellationToken ct = default)
	{
		var activeDeals = _db.Deals.AsNoTracking().Where(d => d.IsActive);

		var totalDeals = await activeDeals.CountAsync(ct);

		var dealsPerAgency = await activeDeals
			.GroupBy(d => d.Agency)
			.Select(g => new { Agency = g.Key, Count = g.Count() })
			.ToDictionaryAsync(x => x.Agency, x => x.Count, ct);

		var averagePrice = totalDeals > 0 ? (int)await activeDeals.AverageAsync(d => d.PricePerPerson, ct) : 0;

		var lowestPrice = totalDeals > 0 ? await activeDeals.MinAsync(d => d.PricePerPerson, ct) : 0;

		var lastUpdated = totalDeals > 0 ? await activeDeals.MaxAsync(d => (DateTime?)d.LastSeen, ct) : null;

		return new DealStats(totalDeals, dealsPerAgency, averagePrice, lowestPrice, lastUpdated);
	}

	public async Task UpsertDealsAsync(string agency, IReadOnlyList<Deal> deals, CancellationToken ct = default)
	{
		var now = DateTime.UtcNow;

		// Get all existing deals for this agency
		var existingDeals = await _db.Deals.Where(d => d.Agency == agency).ToDictionaryAsync(d => d.ExternalId, ct);

		// Deduplicate incoming batch by ExternalId (keep last occurrence)
		var deduplicatedDeals = deals.GroupBy(d => d.ExternalId).Select(g => g.Last()).ToList();

		// Track which external IDs are in the incoming batch
		var incomingExternalIds = new HashSet<string>(deduplicatedDeals.Select(d => d.ExternalId));

		foreach (var deal in deduplicatedDeals)
		{
			if (existingDeals.TryGetValue(deal.ExternalId, out var existing))
			{
				// Update existing deal
				existing.SourceUrl = deal.SourceUrl;
				existing.Destination = deal.Destination;
				existing.Country = deal.Country;
				existing.DepartureDate = deal.DepartureDate;
				existing.ReturnDate = deal.ReturnDate;
				existing.WeekNumber = deal.WeekNumber;
				existing.DurationNights = deal.DurationNights;
				existing.TransportType = deal.TransportType;
				existing.DepartureCity = deal.DepartureCity;
				existing.AccommodationName = deal.AccommodationName;
				existing.AccommodationType = deal.AccommodationType;
				existing.StarRating = deal.StarRating;
				existing.PricePerPerson = deal.PricePerPerson;
				existing.OriginalPrice = deal.OriginalPrice;
				existing.DiscountAmount = deal.DiscountAmount;
				existing.IncludesFlight = deal.IncludesFlight;
				existing.IncludesLiftPass = deal.IncludesLiftPass;
				existing.IncludesMeals = deal.IncludesMeals;
				existing.IncludesTransfer = deal.IncludesTransfer;
				existing.MealDescription = deal.MealDescription;
				existing.LiftPassDays = deal.LiftPassDays;
				existing.LastSeen = now;
				existing.IsActive = true;
			}
			else
			{
				// Insert new deal
				deal.Agency = agency;
				deal.FirstSeen = now;
				deal.LastSeen = now;
				deal.IsActive = true;
				_db.Deals.Add(deal);
			}
		}

		// Mark deals from this agency that are NOT in the incoming batch as inactive
		foreach (var existing in existingDeals.Values)
		{
			if (!incomingExternalIds.Contains(existing.ExternalId))
			{
				existing.IsActive = false;
			}
		}

		await _db.SaveChangesAsync(ct);
	}

	private static DealDto MapToDto(Deal d)
	{
		return new DealDto(
			d.Id,
			d.Agency,
			d.ExternalId,
			d.SourceUrl,
			d.Destination,
			d.Country,
			d.DepartureDate,
			d.ReturnDate,
			d.WeekNumber,
			d.DurationNights,
			d.TransportType,
			d.DepartureCity,
			d.AccommodationName,
			d.AccommodationType,
			d.StarRating,
			d.PricePerPerson,
			d.OriginalPrice,
			d.DiscountAmount,
			d.IncludesFlight,
			d.IncludesLiftPass,
			d.IncludesMeals,
			d.IncludesTransfer,
			d.MealDescription,
			d.LiftPassDays,
			d.FirstSeen,
			d.LastSeen,
			d.IsActive
		);
	}
}
