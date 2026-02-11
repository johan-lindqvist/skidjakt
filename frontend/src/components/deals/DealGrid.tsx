import { DealCard } from './DealCard';
import type { Deal } from '../../types/deal';
import { Loader2 } from 'lucide-react';

interface DealGridProps {
  deals: Deal[];
  isLoading: boolean;
  totalCount: number;
}

export function DealGrid({ deals, isLoading, totalCount }: DealGridProps) {
  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-20">
        <Loader2 className="h-8 w-8 text-sky-400 animate-spin" />
        <span className="ml-3 text-slate-400">Laddar erbjudanden...</span>
      </div>
    );
  }

  if (deals.length === 0) {
    return (
      <div className="text-center py-20">
        <p className="text-lg text-slate-400">Inga erbjudanden hittades</p>
        <p className="text-sm text-slate-500 mt-1">
          Prova att ändra dina filter
        </p>
      </div>
    );
  }

  return (
    <div>
      <p className="text-sm text-slate-400 mb-4">
        {totalCount} erbjudanden hittade
      </p>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {deals.map((deal) => (
          <DealCard key={deal.id} deal={deal} />
        ))}
      </div>
    </div>
  );
}
