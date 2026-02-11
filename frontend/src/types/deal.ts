export interface Deal {
  id: number;
  agency: string;
  externalId: string;
  sourceUrl: string;
  destination: string;
  country: string;
  departureDate: string | null;
  returnDate: string | null;
  weekNumber: number | null;
  durationNights: number;
  transportType: string | null;
  departureCity: string | null;
  accommodationName: string | null;
  accommodationType: string | null;
  starRating: number | null;
  pricePerPerson: number;
  originalPrice: number | null;
  discountAmount: number | null;
  includesFlight: boolean;
  includesLiftPass: boolean;
  includesMeals: boolean;
  includesTransfer: boolean;
  mealDescription: string | null;
  liftPassDays: number | null;
  firstSeen: string;
  lastSeen: string;
  isActive: boolean;
}

export interface DealQuery {
  search?: string;
  agencies?: string[];
  countries?: string[];
  destinations?: string[];
  minPrice?: number;
  maxPrice?: number;
  fromDate?: string;
  toDate?: string;
  transportTypes?: string[];
  sortBy?: string;
  page?: number;
  pageSize?: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface DealFilterOptions {
  agencies: string[];
  countries: string[];
  destinations: string[];
  transportTypes: string[];
  minPrice: number;
  maxPrice: number;
}

export interface DealStats {
  totalDeals: number;
  dealsPerAgency: Record<string, number>;
  averagePrice: number;
  lowestPrice: number;
  lastUpdated: string | null;
}
