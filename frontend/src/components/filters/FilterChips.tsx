import { X } from 'lucide-react';
import { useFilters } from '../../hooks/useFilters';

export function FilterChips() {
	const filters = useFilters();

	const chips: { label: string; onRemove: () => void }[] = [];

	if (filters.search) {
		chips.push({
			label: `Sök: "${filters.search}"`,
			onRemove: () => filters.setSearch(''),
		});
	}
	filters.agencies?.forEach((a) => {
		chips.push({ label: a, onRemove: () => filters.toggleAgency(a) });
	});
	filters.countries?.forEach((c) => {
		chips.push({ label: c, onRemove: () => filters.toggleCountry(c) });
	});
	filters.transportTypes?.forEach((t) => {
		chips.push({ label: t, onRemove: () => filters.toggleTransportType(t) });
	});
	if (filters.minPrice || filters.maxPrice) {
		const label = `${filters.minPrice || 0} - ${filters.maxPrice || '\u221E'} kr`;
		chips.push({ label, onRemove: () => filters.setPriceRange() });
	}
	if (filters.fromDate || filters.toDate) {
		const from = filters.fromDate || '...';
		const to = filters.toDate || '...';
		chips.push({ label: `${from} – ${to}`, onRemove: () => filters.setDateRange() });
	}

	if (chips.length === 0) return null;

	return (
		<div className="flex flex-wrap gap-2">
			{chips.map((chip, i) => (
				<span key={i} className="inline-flex items-center gap-1 px-3 py-1 rounded-full bg-sky-500/20 text-sky-400 text-sm border border-sky-500/30">
					{chip.label}
					<button onClick={chip.onRemove} className="hover:text-white">
						<X className="h-3.5 w-3.5" />
					</button>
				</span>
			))}
		</div>
	);
}
