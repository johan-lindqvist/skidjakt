using Skidjakt.Core.Entities;

namespace Skidjakt.Core.Interfaces;

public interface IDealScraper
{
	string Agency { get; }
	Task<IReadOnlyList<Deal>> ScrapeDealsAsync(CancellationToken ct);
}
