import { createContext, useContext, useState, useCallback, useEffect, type ReactNode } from 'react';
import type { DealQuery } from '../types/deal';

interface FilterState extends DealQuery {
	setSearch: (search: string) => void;
	toggleAgency: (agency: string) => void;
	toggleCountry: (country: string) => void;
	toggleTransportType: (type: string) => void;
	setPriceRange: (min?: number, max?: number) => void;
	setDateRange: (from?: string, to?: string) => void;
	setSortBy: (sort: string) => void;
	setPage: (page: number) => void;
	clearFilters: () => void;
	activeFilterCount: number;
}

const FilterContext = createContext<FilterState | null>(null);

const defaultQuery: DealQuery = {
	page: 1,
	pageSize: 30,
	sortBy: 'price',
};

function parseFiltersFromUrl(): DealQuery {
	const params = new URLSearchParams(window.location.search);
	return {
		search: params.get('search') || undefined,
		agencies: params.getAll('agencies').length ? params.getAll('agencies') : undefined,
		countries: params.getAll('countries').length ? params.getAll('countries') : undefined,
		transportTypes: params.getAll('transportTypes').length ? params.getAll('transportTypes') : undefined,
		minPrice: params.get('minPrice') ? Number(params.get('minPrice')) : undefined,
		maxPrice: params.get('maxPrice') ? Number(params.get('maxPrice')) : undefined,
		fromDate: params.get('fromDate') || undefined,
		toDate: params.get('toDate') || undefined,
		sortBy: params.get('sortBy') || 'price',
		page: params.get('page') ? Number(params.get('page')) : 1,
		pageSize: 30,
	};
}

function syncFiltersToUrl(query: DealQuery) {
	const params = new URLSearchParams();
	if (query.search) params.set('search', query.search);
	if (query.agencies?.length) query.agencies.forEach((a) => params.append('agencies', a));
	if (query.countries?.length) query.countries.forEach((c) => params.append('countries', c));
	if (query.transportTypes?.length) query.transportTypes.forEach((t) => params.append('transportTypes', t));
	if (query.minPrice != null) params.set('minPrice', query.minPrice.toString());
	if (query.maxPrice != null) params.set('maxPrice', query.maxPrice.toString());
	if (query.fromDate) params.set('fromDate', query.fromDate);
	if (query.toDate) params.set('toDate', query.toDate);
	if (query.sortBy && query.sortBy !== 'price') params.set('sortBy', query.sortBy);
	if (query.page && query.page > 1) params.set('page', query.page.toString());

	const search = params.toString();
	const newUrl = search ? `${window.location.pathname}?${search}` : window.location.pathname;
	window.history.replaceState(null, '', newUrl);
}

export function FilterProvider({ children }: { children: ReactNode }) {
	const [query, setQuery] = useState<DealQuery>(() => ({
		...defaultQuery,
		...parseFiltersFromUrl(),
	}));

	useEffect(() => {
		syncFiltersToUrl(query);
	}, [query]);

	const toggleArray = useCallback((current: string[] | undefined, value: string): string[] | undefined => {
		if (!current) return [value];
		const filtered = current.filter((v) => v !== value);
		return filtered.length === current.length ? [...current, value] : filtered.length > 0 ? filtered : undefined;
	}, []);

	const setSearch = useCallback((search: string) => setQuery((q) => ({ ...q, search: search || undefined, page: 1 })), []);
	const toggleAgency = useCallback(
		(agency: string) =>
			setQuery((q) => ({
				...q,
				agencies: toggleArray(q.agencies, agency),
				page: 1,
			})),
		[toggleArray],
	);
	const toggleCountry = useCallback(
		(country: string) =>
			setQuery((q) => ({
				...q,
				countries: toggleArray(q.countries, country),
				page: 1,
			})),
		[toggleArray],
	);
	const toggleTransportType = useCallback(
		(type: string) =>
			setQuery((q) => ({
				...q,
				transportTypes: toggleArray(q.transportTypes, type),
				page: 1,
			})),
		[toggleArray],
	);
	const setPriceRange = useCallback((min?: number, max?: number) => setQuery((q) => ({ ...q, minPrice: min, maxPrice: max, page: 1 })), []);
	const setDateRange = useCallback((from?: string, to?: string) => setQuery((q) => ({ ...q, fromDate: from, toDate: to, page: 1 })), []);
	const setSortBy = useCallback((sort: string) => setQuery((q) => ({ ...q, sortBy: sort, page: 1 })), []);
	const setPage = useCallback((page: number) => setQuery((q) => ({ ...q, page })), []);
	const clearFilters = useCallback(() => setQuery({ ...defaultQuery }), []);

	const activeFilterCount = [
		query.search,
		query.agencies?.length,
		query.countries?.length,
		query.transportTypes?.length,
		query.minPrice,
		query.maxPrice,
		query.fromDate,
		query.toDate,
	].filter(Boolean).length;

	return (
		<FilterContext.Provider
			value={{
				...query,
				setSearch,
				toggleAgency,
				toggleCountry,
				toggleTransportType,
				setPriceRange,
				setDateRange,
				setSortBy,
				setPage,
				clearFilters,
				activeFilterCount,
			}}
		>
			{children}
		</FilterContext.Provider>
	);
}

export function useFilters() {
	const context = useContext(FilterContext);
	if (!context) throw new Error('useFilters must be used within FilterProvider');
	return context;
}
