import { BarChart3, TrendingDown, Clock } from 'lucide-react';
import { useDealStats } from '../../hooks/useDeals';

export function DealStats() {
	const { data: stats } = useDealStats();

	if (!stats) return null;

	const lastUpdated = stats.lastUpdated ? formatTimeAgo(new Date(stats.lastUpdated)) : 'Aldrig';

	return (
		<div className="grid grid-cols-2 md:grid-cols-4 gap-3">
			<StatCard icon={<BarChart3 className="h-4 w-4" />} label="Erbjudanden" value={stats.totalDeals.toString()} />
			<StatCard
				icon={<TrendingDown className="h-4 w-4" />}
				label="Lagsta pris"
				value={stats.lowestPrice > 0 ? `${stats.lowestPrice.toLocaleString('sv-SE')} kr` : '-'}
			/>
			<StatCard
				icon={<BarChart3 className="h-4 w-4" />}
				label="Snittpris"
				value={stats.averagePrice > 0 ? `${stats.averagePrice.toLocaleString('sv-SE')} kr` : '-'}
			/>
			<StatCard icon={<Clock className="h-4 w-4" />} label="Uppdaterat" value={lastUpdated} />
		</div>
	);
}

function StatCard({ icon, label, value }: { icon: React.ReactNode; label: string; value: string }) {
	return (
		<div className="bg-slate-800/50 border border-slate-700 rounded-lg p-3">
			<div className="flex items-center gap-2 text-slate-400 mb-1">
				{icon}
				<span className="text-xs">{label}</span>
			</div>
			<p className="text-lg font-semibold text-white">{value}</p>
		</div>
	);
}

function formatTimeAgo(date: Date): string {
	const now = new Date();
	const diffMs = now.getTime() - date.getTime();
	const diffMin = Math.floor(diffMs / 60000);

	if (diffMin < 1) return 'Just nu';
	if (diffMin < 60) return `${diffMin} min sedan`;
	const diffHours = Math.floor(diffMin / 60);
	if (diffHours < 24) return `${diffHours} tim sedan`;
	const diffDays = Math.floor(diffHours / 24);
	return `${diffDays} dagar sedan`;
}
