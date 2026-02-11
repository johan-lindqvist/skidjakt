import { useQuery } from '@tanstack/react-query';
import { api } from '../services/api';
import type { DealQuery } from '../types/deal';

export function useDeals(query: DealQuery) {
  return useQuery({
    queryKey: ['deals', query],
    queryFn: () => api.getDeals(query),
    staleTime: 60_000,
  });
}

export function useDeal(id: number) {
  return useQuery({
    queryKey: ['deal', id],
    queryFn: () => api.getDeal(id),
    enabled: id > 0,
  });
}

export function useFiltersOptions() {
  return useQuery({
    queryKey: ['filters'],
    queryFn: () => api.getFilters(),
    staleTime: 300_000,
  });
}

export function useDealStats() {
  return useQuery({
    queryKey: ['stats'],
    queryFn: () => api.getStats(),
    staleTime: 60_000,
  });
}
