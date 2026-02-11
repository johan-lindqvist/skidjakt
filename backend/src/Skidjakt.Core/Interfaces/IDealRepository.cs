using Skidjakt.Core.DTOs;
using Skidjakt.Core.Entities;

namespace Skidjakt.Core.Interfaces;

public interface IDealRepository
{
	Task<PagedResult<DealDto>> GetDealsAsync(DealQuery query, CancellationToken ct = default);
	Task<DealDto?> GetDealByIdAsync(int id, CancellationToken ct = default);
	Task<DealFilterOptions> GetFilterOptionsAsync(CancellationToken ct = default);
	Task<DealStats> GetStatsAsync(CancellationToken ct = default);
	Task UpsertDealsAsync(string agency, IReadOnlyList<Deal> deals, CancellationToken ct = default);
}
