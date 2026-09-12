import type { SchoolClassDropdownValue } from './school-class';
import { LocationDropdownValue } from './location';

export type CategoryStockSummaryDto = {
  categoryId: string;
  categoryName: string;
  quantity: number;
  itemCount: number;
};

export type ClassStockByCategoryDto = {
  categories: CategoryStockSummaryDto[];
  totalQuantity: number;
  totalItemCount: number;
  totalCategoryCount: number;
};

export type GoodsReceiptCostPointDto = {
  id: string;
  receivedAt: string;
  totalAmount: number;
  supplierReference?: string | null;
};

export type ClassGoodsReceiptCostsDto = {
  points: GoodsReceiptCostPointDto[];
  receiptCount: number;
  totalAmount: number;
  averageAmount: number;
};

export type ClassAnalysisFilterFormData = {
  schoolClass: SchoolClassDropdownValue;
  location: LocationDropdownValue;
  startDate: Date | null;
  endDate: Date | null;
};
export type ClassAnalysisFilter = ClassAnalysisFilterFormData;
