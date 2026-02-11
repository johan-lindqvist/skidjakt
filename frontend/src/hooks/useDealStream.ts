import { useEffect } from 'react';
import { useQueryClient } from '@tanstack/react-query';

export function useDealStream() {
	const queryClient = useQueryClient();

	useEffect(() => {
		const eventSource = new EventSource('/api/deals/stream');

		eventSource.addEventListener('deals-updated', () => {
			queryClient.invalidateQueries({ queryKey: ['deals'] });
			queryClient.invalidateQueries({ queryKey: ['stats'] });
			queryClient.invalidateQueries({ queryKey: ['filters'] });
		});

		eventSource.onerror = () => {
			eventSource.close();
			// Reconnect after 5 seconds
			setTimeout(() => {
				// Component will re-mount and reconnect
			}, 5000);
		};

		return () => eventSource.close();
	}, [queryClient]);
}
