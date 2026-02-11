import { useState, useRef, useCallback } from 'react';
import { Mountain, RefreshCw } from 'lucide-react';
import { api } from '../../services/api';

export function Header() {
	const [scraping, setScraping] = useState(false);
	const [scrapeMessage, setScrapeMessage] = useState<string | null>(null);
	const [isAdmin, setIsAdmin] = useState(false);
	const clickTimestamps = useRef<number[]>([]);

	const handleLogoClick = useCallback(() => {
		const now = Date.now();
		clickTimestamps.current = clickTimestamps.current.filter((t) => now - t < 2000);
		clickTimestamps.current.push(now);
		if (clickTimestamps.current.length >= 5) {
			setIsAdmin(true);
			clickTimestamps.current = [];
		}
	}, []);

	const handleScrape = async () => {
		setScraping(true);
		setScrapeMessage(null);
		try {
			const res = await api.triggerScrape();
			if (res.status === 409) {
				setScrapeMessage('Skrapning pågår redan');
				setTimeout(() => setScrapeMessage(null), 3000);
			}
		} finally {
			setScraping(false);
		}
	};

	return (
		<header className="bg-slate-950 border-b border-slate-800">
			<div className="max-w-7xl mx-auto px-4 py-4 flex items-center gap-3">
				<Mountain className="h-8 w-8 text-sky-400 select-none" onClick={handleLogoClick} />
				<div>
					<h1 className="text-2xl font-bold text-white tracking-tight">Skidjakt</h1>
					<p className="text-sm text-slate-400">Hitta billigaste skidresorna</p>
				</div>
				{isAdmin && (
					<div className="ml-auto flex items-center gap-2">
						{scrapeMessage && <span className="text-xs text-amber-400">{scrapeMessage}</span>}
						<button
							onClick={handleScrape}
							disabled={scraping}
							title="Uppdatera"
							className="p-2 rounded-lg text-slate-400 hover:text-sky-400 hover:bg-slate-800 transition-colors disabled:opacity-50"
						>
							<RefreshCw className={`h-5 w-5 ${scraping ? 'animate-spin' : ''}`} />
						</button>
					</div>
				)}
			</div>
		</header>
	);
}
