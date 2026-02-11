import type { Deal, DealQuery, DealFilterOptions, DealStats, PagedResult } from '../types/deal';

const API_BASE = '/api';

function buildQueryString(query: DealQuery): string {
	const params = new URLSearchParams();
	if (query.search) params.set('search', query.search);
	if (query.agencies?.length) query.agencies.forEach((a) => params.append('agencies', a));
	if (query.countries?.length) query.countries.forEach((c) => params.append('countries', c));
	if (query.destinations?.length) query.destinations.forEach((d) => params.append('destinations', d));
	if (query.minPrice != null) params.set('minPrice', query.minPrice.toString());
	if (query.maxPrice != null) params.set('maxPrice', query.maxPrice.toString());
	if (query.fromDate) params.set('fromDate', query.fromDate);
	if (query.toDate) params.set('toDate', query.toDate);
	if (query.transportTypes?.length) query.transportTypes.forEach((t) => params.append('transportTypes', t));
	if (query.sortBy) params.set('sortBy', query.sortBy);
	if (query.page) params.set('page', query.page.toString());
	if (query.pageSize) params.set('pageSize', query.pageSize.toString());
	return params.toString();
}

async function fetchJson<T>(url: string): Promise<T> {
	const res = await fetch(url);
	if (!res.ok) throw new Error(`API error: ${res.status}`);
	return res.json();
}

export const api = {
	getDeals: (query: DealQuery = {}): Promise<PagedResult<Deal>> => fetchJson(`${API_BASE}/deals?${buildQueryString(query)}`),

	getDeal: (id: number): Promise<Deal> => fetchJson(`${API_BASE}/deals/${id}`),

	getFilters: (): Promise<DealFilterOptions> => fetchJson(`${API_BASE}/deals/filters`),

	getStats: (): Promise<DealStats> => fetchJson(`${API_BASE}/deals/stats`),

	triggerScrape: (): Promise<Response> =>
		fetch(`${API_BASE}/scrape/trigger`, { method: 'POST' }).then((res) => {
			if (!res.ok && res.status !== 409) throw new Error(`API error: ${res.status}`);
			return res;
		}),
};
