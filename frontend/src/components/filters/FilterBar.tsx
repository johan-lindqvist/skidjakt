import { useFilters } from '../../hooks/useFilters';
import { useFiltersOptions } from '../../hooks/useDeals';
import { SlidersHorizontal } from 'lucide-react';

export function FilterBar() {
  const filters = useFilters();
  const { data: options } = useFiltersOptions();

  if (!options) return null;

  return (
    <div className="bg-slate-800/50 border border-slate-700 rounded-xl p-4">
      <div className="flex items-center gap-2 mb-4">
        <SlidersHorizontal className="h-4 w-4 text-sky-400" />
        <span className="text-sm font-medium text-white">Filter</span>
        {filters.activeFilterCount > 0 && (
          <button
            onClick={filters.clearFilters}
            className="ml-auto text-xs text-sky-400 hover:text-sky-300"
          >
            Rensa alla
          </button>
        )}
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {/* Agencies */}
        <div>
          <h4 className="text-xs font-medium text-slate-400 uppercase tracking-wider mb-2">
            Resebyrå
          </h4>
          <div className="space-y-1.5">
            {options.agencies.map((agency) => (
              <label
                key={agency}
                className="flex items-center gap-2 cursor-pointer group"
              >
                <input
                  type="checkbox"
                  checked={filters.agencies?.includes(agency) ?? false}
                  onChange={() => filters.toggleAgency(agency)}
                  className="rounded border-slate-600 bg-slate-700 text-sky-500 focus:ring-sky-500 focus:ring-offset-0"
                />
                <span className="text-sm text-slate-300 group-hover:text-white capitalize">
                  {agency}
                </span>
              </label>
            ))}
          </div>
        </div>

        {/* Countries */}
        <div>
          <h4 className="text-xs font-medium text-slate-400 uppercase tracking-wider mb-2">
            Land
          </h4>
          <div className="space-y-1.5">
            {options.countries.map((country) => (
              <label
                key={country}
                className="flex items-center gap-2 cursor-pointer group"
              >
                <input
                  type="checkbox"
                  checked={filters.countries?.includes(country) ?? false}
                  onChange={() => filters.toggleCountry(country)}
                  className="rounded border-slate-600 bg-slate-700 text-sky-500 focus:ring-sky-500 focus:ring-offset-0"
                />
                <span className="text-sm text-slate-300 group-hover:text-white">
                  {country}
                </span>
              </label>
            ))}
          </div>
        </div>

        {/* Transport */}
        <div>
          <h4 className="text-xs font-medium text-slate-400 uppercase tracking-wider mb-2">
            Resa
          </h4>
          <div className="space-y-1.5">
            {options.transportTypes.map((type) => (
              <label
                key={type}
                className="flex items-center gap-2 cursor-pointer group"
              >
                <input
                  type="checkbox"
                  checked={filters.transportTypes?.includes(type) ?? false}
                  onChange={() => filters.toggleTransportType(type)}
                  className="rounded border-slate-600 bg-slate-700 text-sky-500 focus:ring-sky-500 focus:ring-offset-0"
                />
                <span className="text-sm text-slate-300 group-hover:text-white">
                  {type}
                </span>
              </label>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
