export type ClassStockByCategorySeriesDto = {
  name: string;
  data: number[];
};

export type ClassStockByCategoryChartDto = {
  labels: string[];
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
