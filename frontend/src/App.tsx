import { Header } from './components/layout/Header';
import { Footer } from './components/layout/Footer';
import { SearchInput } from './components/search/SearchInput';
import { FilterBar } from './components/filters/FilterBar';
import { FilterChips } from './components/filters/FilterChips';
import { SortSelector } from './components/filters/SortSelector';
import { DealGrid } from './components/deals/DealGrid';
import { DealStats } from './components/deals/DealStats';
import { Pagination } from './components/deals/Pagination';
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

        <DealStats />

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

        <Pagination
          page={filters.page || 1}
          totalPages={data?.totalPages || 1}
          onPageChange={filters.setPage}
        />
      </main>

      <Footer />
    </div>
  );
}

export default function App() {
  return <AppContent />;
}
