export type ClassStockByCategorySeriesDto = {
  name: string;
  data: number[];
};

/** Stacked-bar chart data: one bar per label, one stacked segment (series) per category. */
export type ClassStockByCategoryChartDto = {
  labels: string[];
  /** Ids of the entities in `labels`, index-aligned (location ids, or item ids in a location drill-down). */
  labelIds: string[];
  series: ClassStockByCategorySeriesDto[];
};

export type ClassItemStockEvolutionPointDto = {
  date: string;
  cumulativeQuantity: number;
};

export type ClassItemStockEvolutionDto = {
  /** Item fields are null when the series covers all items. */
  itemId: string | null;
  itemName: string | null;
  sku: string | null;
  unit: string | null;
  points: ClassItemStockEvolutionPointDto[];
};

export type DailyConsumptionPointDto = {
  date: string;
  quantity: number;
  value: number;
};

export type ClassDailyConsumptionDto = {
  classId: string;
  fromDate: string | null;
  toDate: string | null;
  points: DailyConsumptionPointDto[];
  totalQuantity: number;
  totalValue: number;
  averageQuantity: number;
  averageValue: number;
};

export type ClassStockEvolutionFilter = {
  itemId: string | null;
  locationId: string | null;
};

export type ClassDailyConsumptionFilter = {
  itemId: string | null;
  locationId: string | null;
  categoryId: string | null;
};

export type PurchaseStatisticsScope = 'Last90Days' | 'Last365Days' | 'Class';

export type PurchaseStatisticDto = {
  itemId: string;
  itemName: string;
  sku: string | null;
  unit: string;
  categoryName: string;
  totalQuantity: number;
  totalValue: number;
  purchaseCount: number;
  averageQuantity: number;
  averageUnitPrice: number;
  lastPurchasedAt: string;
};

export type TopPurchasesDto = {
  computedAt: string | null;
  byQuantity: PurchaseStatisticDto[];
  byValue: PurchaseStatisticDto[];
  byFrequency: PurchaseStatisticDto[];
};

export type TopPurchasesFilter = {
  scope: PurchaseStatisticsScope;
  classId: string | null;
  categoryId: string | null;
  top?: number;
};

export type ClassSummaryDto = {
  orderCount: number;
  totalOrders: number;
  donationCount: number;
  totalDonations: number;
  totalExpenses: number;
  currentStockValue: number;
};

export type ClassEvolutionPointDto = {
  /** Monday of the week, `yyyy-MM-dd`. */
  weekStart: string;
  orders: number;
  donations: number;
  expenses: number;
};

export type ClassEvolutionDto = {
  points: ClassEvolutionPointDto[];
};
