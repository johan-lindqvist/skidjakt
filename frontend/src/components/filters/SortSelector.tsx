import { ArrowUpDown } from 'lucide-react';
import { useFilters } from '../../hooks/useFilters';

const sortOptions = [
	{ value: 'price', label: 'Lägst pris' },
	{ value: 'price_desc', label: 'Högst pris' },
	{ value: 'date', label: 'Närmast datum' },
	{ value: 'discount', label: 'Störst rabatt' },
	{ value: 'newest', label: 'Nyast' },
];

export function SortSelector() {
	const { sortBy, setSortBy } = useFilters();

	return (
		<div className="flex items-center gap-2">
			<ArrowUpDown className="h-4 w-4 text-slate-500" />
			<select
				value={sortBy || 'price'}
				onChange={(e) => setSortBy(e.target.value)}
				className="bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:border-sky-500"
			>
				{sortOptions.map((opt) => (
					<option key={opt.value} value={opt.value}>
						{opt.label}
					</option>
				))}
			</select>
		</div>
	);
}
