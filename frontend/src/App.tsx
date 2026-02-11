import { Header } from './components/layout/Header';
import { Footer } from './components/layout/Footer';
import { SearchInput } from './components/search/SearchInput';
import { FilterBar } from './components/filters/FilterBar';
import { FilterChips } from './components/filters/FilterChips';
import { SortSelector } from './components/filters/SortSelector';
import { DealGrid } from './components/deals/DealGrid';
import { useDeals } from './hooks/useDeals';
import { useFilters } from './hooks/useFilters';
import { useDealStream } from './hooks/useDealStream';

function AppContent() {
  const filters = useFilters();
  const { data, isLoading } = useDeals(filters);
  useDealStream();

  return (
    <div className="min-h-screen bg-slate-900 flex flex-col">
      <Header />

      <main className="flex-1 max-w-7xl mx-auto w-full px-4 py-6 space-y-5">
        <SearchInput
          value={filters.search || ''}
          onChange={filters.setSearch}
        />

        <FilterBar />

        <div className="flex items-center justify-between gap-4 flex-wrap">
          <FilterChips />
          <SortSelector />
        </div>

        <DealGrid
          deals={data?.items || []}
          isLoading={isLoading}
          totalCount={data?.totalCount || 0}
        />

        {/* Pagination */}
        {data && data.totalPages > 1 && (
          <div className="flex items-center justify-center gap-2 pt-4">
            <button
              onClick={() => filters.setPage(filters.page! - 1)}
              disabled={filters.page === 1}
              className="px-4 py-2 text-sm bg-slate-800 border border-slate-700 rounded-lg text-white disabled:opacity-50 disabled:cursor-not-allowed hover:border-sky-500 transition-colors"
            >
              Föregående
            </button>
            <span className="text-sm text-slate-400">
              Sida {filters.page} av {data.totalPages}
            </span>
            <button
              onClick={() => filters.setPage(filters.page! + 1)}
              disabled={filters.page === data.totalPages}
              className="px-4 py-2 text-sm bg-slate-800 border border-slate-700 rounded-lg text-white disabled:opacity-50 disabled:cursor-not-allowed hover:border-sky-500 transition-colors"
            >
              Nästa
            </button>
          </div>
        )}
      </main>

      <Footer />
    </div>
  );
}

export default function App() {
  return <AppContent />;
}
