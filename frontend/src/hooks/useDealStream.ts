import { useEffect, useRef } from 'react';
import { useQueryClient } from '@tanstack/react-query';

export function useDealStream() {
	const queryClient = useQueryClient();
	const retryTimeout = useRef<ReturnType<typeof setTimeout>>(null);

	useEffect(() => {
		let eventSource: EventSource | null = null;
		let cancelled = false;

		function connect() {
			if (cancelled) return;

			eventSource = new EventSource('/api/deals/stream');

			eventSource.addEventListener('deals-updated', () => {
				queryClient.invalidateQueries({ queryKey: ['deals'] });
				queryClient.invalidateQueries({ queryKey: ['stats'] });
				queryClient.invalidateQueries({ queryKey: ['filters'] });
			});

			eventSource.onerror = () => {
				eventSource?.close();
				eventSource = null;
				// Reconnect after 5 seconds
				if (!cancelled) {
					retryTimeout.current = setTimeout(connect, 5000);
				}
			};
		}

		connect();

		return () => {
			cancelled = true;
			eventSource?.close();
			if (retryTimeout.current) {
				clearTimeout(retryTimeout.current);
			}
		};
	}, [queryClient]);
}
