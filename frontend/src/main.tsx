import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { FilterProvider } from './hooks/useFilters';
import App from './App';
import './index.css';

const queryClient = new QueryClient({
	defaultOptions: {
		queries: {
			retry: 1,
			refetchOnWindowFocus: false,
		},
	},
});

createRoot(document.getElementById('root')!).render(
	<StrictMode>
		<QueryClientProvider client={queryClient}>
			<FilterProvider>
				<App />
			</FilterProvider>
		</QueryClientProvider>
	</StrictMode>,
);
