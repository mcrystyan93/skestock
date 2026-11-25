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
  lastUpdatedAt: string | null;
  unit: string;
  isPerishable: boolean;
  isExpired: boolean;
  expiredQuantity: number;
  quantity: number;
  isLowStock: boolean;
  hideWhenZeroStock: boolean;
  imageFileId?: string | null;
};

/** Mirrors src/Application/Features/Stock/Models/StockDto.cs (StockReportDto). */
export type StockReportDto = {
  items: StockItemDto[];
  hasExpiredItems: boolean;
};

/** One item with its per-location stock lines, in the order received. */
export type StockItemGroup = {
  itemId: string;
  /** Identity fields (name, sku, unit, image) are the same on every location line. */
  item: StockItemDto;
  locations: StockItemDto[];
  totalQuantity: number;
  /** Most recent update across the locations. */
  lastUpdatedAt: string | null;
  isLowStock: boolean;
  isExpired: boolean;
};

export function groupStockByItem(items: readonly StockItemDto[]): StockItemGroup[] {
  const groups = new Map<string, StockItemGroup>();

  for (const line of items) {
    const group = groups.get(line.itemId);
    if (group) {
      group.locations.push(line);
      group.totalQuantity += line.quantity;
      if (line.lastUpdatedAt && (!group.lastUpdatedAt || line.lastUpdatedAt > group.lastUpdatedAt))
        group.lastUpdatedAt = line.lastUpdatedAt;
      group.isLowStock ||= line.isLowStock;
      group.isExpired ||= line.isExpired;
    } else {
      groups.set(line.itemId, {
        itemId: line.itemId,
        item: line,
        locations: [line],
        totalQuantity: line.quantity,
        lastUpdatedAt: line.lastUpdatedAt,
        isLowStock: line.isLowStock,
        isExpired: line.isExpired
      });
    }
  }

  return Array.from(groups.values());
}

/** Row of the desktop stock table: a category header, or one item with all its locations. */
export type StockItemCategoryGroup = {
  categoryId: string;
  categoryName: string;
  categoryIcon?: IconPickerValue | null;
  /** Every stock line of the category (header rows only). */
  items: StockItemDto[];
  /** The item with its locations (item rows only). */
  group: StockItemGroup | null;
  isHeader: boolean;
  trackKey: string;
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

/** Mirrors src/Application/Features/Stock/Models/StockRequests.cs (BulkAdjustStockLine). */
export type BulkAdjustStockLine = {
  itemId: string;
  locationId: string;
  actualQuantity: number;
  /** Stock total shown to the counter; the server rejects the count if it changed since. */
  expectedQuantity?: number;
};

/** Body of `POST /api/Stock/class/{classId}/bulk-adjust` (BulkAdjustStockRequest). */
export type BulkAdjustStockRequest = {
  reason: AdjustmentReason;
  lines: BulkAdjustStockLine[];
};

/** Mirrors src/Application/Features/Stock/Models/StockRequests.cs (RemoveExpiredStockRequest). */
export type RemoveExpiredStockRequest = {
  classId: string;
  itemId: string;
  locationId: string;
};

/** Mirrors src/Application/Features/Stock/Models/StockRequests.cs (ExtendExpiredStockExpiryRequest). */
export type ExtendExpiredStockExpiryRequest = RemoveExpiredStockRequest & {
  extensionDays: number;
};

export const MAX_EXPIRY_EXTENSION_DAYS = 365;

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
  | 'lastUpdatedAt'
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
  lastUpdatedAt: {
    label: 'Ultima actualizare',
    value: 'lastUpdatedAt',
    fieldType: 'date'
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
