namespace Skidjakt.Core.Entities;

public class Deal
{
	public int Id { get; set; }
	public required string Agency { get; set; }
	public required string ExternalId { get; set; }
	public required string SourceUrl { get; set; }
	public required string Destination { get; set; }
	public required string Country { get; set; }
	public DateOnly? DepartureDate { get; set; }
	public DateOnly? ReturnDate { get; set; }
	public int? WeekNumber { get; set; }
	public int DurationNights { get; set; }
	public string? TransportType { get; set; }
	public string? DepartureCity { get; set; }
	public string? AccommodationName { get; set; }
	public string? AccommodationType { get; set; }
	public int? StarRating { get; set; }
	public required int PricePerPerson { get; set; }
	public int? OriginalPrice { get; set; }
	public int? DiscountAmount { get; set; }
	public bool IncludesFlight { get; set; }
	public bool IncludesLiftPass { get; set; }
	public bool IncludesMeals { get; set; }
	public bool IncludesTransfer { get; set; }
	public string? MealDescription { get; set; }
	public int? LiftPassDays { get; set; }
	public DateTime FirstSeen { get; set; }
	public DateTime LastSeen { get; set; }
	public bool IsActive { get; set; } = true;
}
