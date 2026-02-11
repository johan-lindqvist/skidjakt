import { useState } from 'react';
import { Mountain, RefreshCw } from 'lucide-react';
import { api } from '../../services/api';

export function Header() {
	const [scraping, setScraping] = useState(false);

	const handleScrape = async () => {
		setScraping(true);
		try {
			await api.triggerScrape();
		} finally {
			setScraping(false);
		}
	};

	return (
		<header className="bg-slate-950 border-b border-slate-800">
			<div className="max-w-7xl mx-auto px-4 py-4 flex items-center gap-3">
				<Mountain className="h-8 w-8 text-sky-400" />
				<div>
					<h1 className="text-2xl font-bold text-white tracking-tight">Skidjakt</h1>
					<p className="text-sm text-slate-400">Hitta billigaste skidresorna</p>
				</div>
				<button
					onClick={handleScrape}
					disabled={scraping}
					title="Uppdatera"
					className="ml-auto p-2 rounded-lg text-slate-400 hover:text-sky-400 hover:bg-slate-800 transition-colors disabled:opacity-50"
				>
					<RefreshCw className={`h-5 w-5 ${scraping ? 'animate-spin' : ''}`} />
				</button>
			</div>
		</header>
	);
}
