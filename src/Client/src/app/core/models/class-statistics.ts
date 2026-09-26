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
  itemId: string;
  itemName: string;
  sku: string | null;
  unit: string;
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
