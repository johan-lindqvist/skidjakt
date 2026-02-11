using Microsoft.EntityFrameworkCore;
using Skidjakt.Core.Entities;

namespace Skidjakt.Infrastructure.Data;

public class SkidjaktDbContext : DbContext
{
	public DbSet<Deal> Deals => Set<Deal>();

	public SkidjaktDbContext(DbContextOptions<SkidjaktDbContext> options)
		: base(options) { }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<Deal>(entity =>
		{
			entity.HasKey(d => d.Id);
			entity.HasIndex(d => new { d.Agency, d.ExternalId }).IsUnique();
			entity.HasIndex(d => d.IsActive);
			entity.HasIndex(d => d.PricePerPerson);
			entity.HasIndex(d => d.DepartureDate);
			entity.HasIndex(d => d.Country);
			entity.HasIndex(d => d.Destination);

			entity.Property(d => d.Agency).HasMaxLength(50);
			entity.Property(d => d.ExternalId).HasMaxLength(200);
			entity.Property(d => d.SourceUrl).HasMaxLength(500);
			entity.Property(d => d.Destination).HasMaxLength(200);
			entity.Property(d => d.Country).HasMaxLength(100);
			entity.Property(d => d.TransportType).HasMaxLength(50);
			entity.Property(d => d.DepartureCity).HasMaxLength(100);
			entity.Property(d => d.AccommodationName).HasMaxLength(300);
			entity.Property(d => d.AccommodationType).HasMaxLength(100);
			entity.Property(d => d.MealDescription).HasMaxLength(200);
		});
	}
}
