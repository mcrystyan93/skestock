import { ColumnFilter, TableColumnDefinition } from './pagination';
import type { IconPickerValue } from '@ske/shared/icons';

/** Mirrors src/Application/Features/Stock/Models/StockDto.cs (StockItemDto). */
export type StockItemDto = {
  itemId: string;
  itemName: string;
  sku?: string | null;
  categoryId: string;
  categoryName: string;
  categoryIcon?: IconPickerValue | null;
  locationId: string;
  locationName: string;
  unit: string;
  isPerishable: boolean;
  isExpired: boolean;
  expiredQuantity: number;
  quantity: number;
  isLowStock: boolean;
  hideWhenZeroStock: boolean;
};

/** Mirrors src/Application/Features/Stock/Models/StockDto.cs (StockReportDto). */
export type StockReportDto = {
  items: StockItemDto[];
  hasExpiredItems: boolean;
};

export type StockItemCategoryGroup = {
  categoryId: string;
  categoryName: string;
  categoryIcon?: IconPickerValue | null;
};

/**
 * Mirrors the route/body contract of `POST /api/Stock/class/{classId}`
 * (src/Web/Endpoints/Stock.cs, GetClassLocationStockRequest).
 */
export type GetClassLocationStockRequest = {
  classId: string;
  filters: ColumnFilter[];
  /** Optional - when provided, only items whose name contains this term (case-insensitive) are included. */
  searchTerm?: string | null;
  includeHidden: boolean;
  lowStockOnly: boolean;
  expiredOnly: boolean;
};

/**
 * Mirrors src/Domain/Enums/AdjustmentReason.cs. No JsonStringEnumConverter is registered for
 * this API, so enums serialize as their ordinal (numeric) value on the wire — values here must
 * stay in the same declaration order as the C# enum.
 */
export enum AdjustmentReason {
  Adjustment = 0,
  Miscount = 1,
  Damaged = 2,
  Expired = 3,
  Found = 4,
  Other = 5,
}

export const ADJUSTMENT_REASON_OPTIONS: Array<{ label: string; value: AdjustmentReason }> = [
  { label: 'Ajustare', value: AdjustmentReason.Adjustment },
  { label: 'Numărătoare greșită', value: AdjustmentReason.Miscount },
  { label: 'Deteriorat', value: AdjustmentReason.Damaged },
  { label: 'Expirat', value: AdjustmentReason.Expired },
  { label: 'Găsit', value: AdjustmentReason.Found },
  { label: 'Altul', value: AdjustmentReason.Other }
];

/** Mirrors src/Application/Features/Stock/Models/StockRequests.cs (AdjustStockRequest). */
export type AdjustStockRequest = {
  classId: string;
  itemId: string;
  locationId: string;
  actualQuantity: number;
  reason: AdjustmentReason;
};

/** Mirrors src/Application/Features/Stock/Models/StockRequests.cs (RemoveExpiredStockRequest). */
export type RemoveExpiredStockRequest = {
  classId: string;
  itemId: string;
  locationId: string;
};

/** Mirrors the move-stock request handled by src/Web/Endpoints/Stock.cs. */
export type MoveStockRequest = {
  classId: string;
  itemId: string;
  sourceLocationId: string;
  destinationLocationId: string;
  quantity: number;
};

export type SetClassItemStockVisibilityRequest = {
  hideWhenZeroStock: boolean;
};

export type StockTableColumn =
  | 'itemName'
  | 'locationName'
  | 'unit'
  | 'isPerishable'
  | 'quantity'
  | 'isLowStock';

export const STOCK_TABLE_COLUMNS: TableColumnDefinition<StockTableColumn> = {
  itemName: {
    label: 'Articol',
    value: 'itemName',
    fieldType: 'string'
  },
  locationName: {
    label: 'Locatie',
    value: 'locationName',
    fieldType: 'string'
  },
  unit: {
    label: 'Unitate',
    value: 'unit',
    fieldType: 'string'
  },
  isPerishable: {
    label: 'Perisabil',
    value: 'isPerishable',
    fieldType: 'boolean'
  },
  quantity: {
    label: 'Cantitate',
    value: 'quantity',
    fieldType: 'number'
  },
  isLowStock: {
    label: 'Stoc redus',
    value: 'isLowStock',
    fieldType: 'boolean'
  }
};
