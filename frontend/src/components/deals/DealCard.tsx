import { Plane, Ticket, UtensilsCrossed, Bus, ExternalLink, Calendar, Moon, Users, MapPin } from 'lucide-react';
import type { Deal } from '../../types/deal';

const agencyColors: Record<string, string> = {
	skilink: 'bg-blue-500/20 text-blue-400 border-blue-500/30',
	alpresor: 'bg-red-500/20 text-red-400 border-red-500/30',
	nortlander: 'bg-green-500/20 text-green-400 border-green-500/30',
	slopestar: 'bg-purple-500/20 text-purple-400 border-purple-500/30',
};

const agencyNames: Record<string, string> = {
	skilink: 'Skilink',
	alpresor: 'STS Alpresor',
	nortlander: 'Nortlander',
	slopestar: 'Slopestar',
};

interface DealCardProps {
	deal: Deal;
}

export function DealCard({ deal }: DealCardProps) {
	const hasDiscount = deal.originalPrice && deal.originalPrice > deal.pricePerPerson;
	const discountPercent = hasDiscount ? Math.round(((deal.originalPrice! - deal.pricePerPerson) / deal.originalPrice!) * 100) : 0;

	return (
		<div className="bg-slate-800 border border-slate-700 rounded-xl overflow-hidden hover:border-sky-500/50 transition-colors group">
			<div className="p-5">
				{/* Top row: agency + discount */}
				<div className="flex items-center justify-between mb-3">
					<span
						className={`text-xs font-medium px-2.5 py-1 rounded-full border ${agencyColors[deal.agency] || 'bg-slate-700 text-slate-300 border-slate-600'}`}
					>
						{agencyNames[deal.agency] || deal.agency}
					</span>
					{hasDiscount && (
						<span className="text-xs font-bold px-2.5 py-1 rounded-full bg-emerald-500/20 text-emerald-400 border border-emerald-500/30">
							-{discountPercent}%
						</span>
					)}
				</div>

				{/* Destination */}
				<h3 className="text-lg font-semibold text-white mb-1">{deal.destination}</h3>
				<p className="text-sm text-slate-400 mb-3">{deal.country}</p>

				{/* Date & duration */}
				<div className="flex items-center gap-4 text-sm text-slate-300 mb-4">
					{deal.weekNumber && (
						<span className="flex items-center gap-1.5">
							<Calendar className="h-4 w-4 text-slate-500" />
							Vecka {deal.weekNumber}
						</span>
					)}
					{deal.departureDate && !deal.weekNumber && (
						<span className="flex items-center gap-1.5">
							<Calendar className="h-4 w-4 text-slate-500" />
							{new Date(deal.departureDate).toLocaleDateString('sv-SE', {
								day: 'numeric',
								month: 'short',
							})}
						</span>
					)}
					<span className="flex items-center gap-1.5">
						<Moon className="h-4 w-4 text-slate-500" />
						{deal.durationNights} nätter
					</span>
				</div>

				{/* Room type & person count */}
				{(deal.roomType || deal.personCount) && (
					<div className="flex items-center gap-3 text-sm text-slate-300 mb-3">
						{deal.personCount && (
							<span className="flex items-center gap-1.5">
								<Users className="h-4 w-4 text-slate-500" />
								{deal.personCount} pers.
							</span>
						)}
						{deal.roomType && <span className="text-xs text-slate-500">{deal.roomType}</span>}
					</div>
				)}

				{/* Inclusions */}
				<div className="flex items-center gap-3 mb-3">
					{deal.includesFlight && (
						<span className="flex items-center gap-1 text-xs text-slate-400" title="Flyg ingår">
							<Plane className="h-3.5 w-3.5" /> Flyg
						</span>
					)}
					{deal.includesLiftPass && (
						<span className="flex items-center gap-1 text-xs text-slate-400" title="Liftkort ingår">
							<Ticket className="h-3.5 w-3.5" /> Liftkort
						</span>
					)}
					{deal.includesMeals && (
						<span className="flex items-center gap-1 text-xs text-slate-400" title="Måltider ingår">
							<UtensilsCrossed className="h-3.5 w-3.5" /> {deal.mealDescription || 'Måltider'}
						</span>
					)}
					{deal.includesTransfer && (
						<span className="flex items-center gap-1 text-xs text-slate-400" title="Transfer ingår">
							<Bus className="h-3.5 w-3.5" /> Transfer
						</span>
					)}
				</div>

				{/* Distances */}
				{(deal.distanceToLiftMeters || deal.distanceToSlopeMeters || deal.distanceToCentreMeters) && (
					<div className="flex items-center gap-3 text-xs text-slate-500 mb-4">
						<MapPin className="h-3.5 w-3.5 flex-shrink-0" />
						{deal.distanceToLiftMeters != null && <span>Lift: {deal.distanceToLiftMeters} m</span>}
						{deal.distanceToSlopeMeters != null && <span>Pist: {deal.distanceToSlopeMeters} m</span>}
						{deal.distanceToCentreMeters != null && <span>Centrum: {deal.distanceToCentreMeters} m</span>}
					</div>
				)}

				{/* Price */}
				<div className="flex items-end justify-between">
					<div>
						{hasDiscount && <span className="text-sm text-slate-500 line-through mr-2">{deal.originalPrice!.toLocaleString('sv-SE')} kr</span>}
						<span className="text-2xl font-bold text-sky-400">{deal.pricePerPerson.toLocaleString('sv-SE')} kr</span>
						<span className="text-xs text-slate-500 ml-1">/person</span>
					</div>
					<a
						href={deal.sourceUrl}
						target="_blank"
						rel="noopener noreferrer"
						className="flex items-center gap-1.5 text-sm font-medium text-sky-400 hover:text-sky-300 transition-colors"
					>
						Se erbjudande
						<ExternalLink className="h-3.5 w-3.5" />
					</a>
				</div>
			</div>
		</div>
	);
}
