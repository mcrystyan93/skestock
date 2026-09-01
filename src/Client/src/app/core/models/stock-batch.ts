import { BasePaginationFilter, ColumnFilter, prioritizeSort, TableColumnDefinition } from './pagination';
import { PAGINATION_PAGE_SIZE } from './category';

/**
 * Row shape for the paginated stock-batch list. Mirrors
 * src/Application/Features/StockBatches/Models/StockBatchDto.cs (StockBatchListItemDto).
 */
export type StockBatchListItemDto = {
  id: number;
  itemId: number;
  itemName: string;
  locationId: number;
  locationName: string;
  goodsReceiptId?: number | null;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  expiryDate?: string | null;
  receivedDate: string;
  createdDate: string;
};

/** Mirrors src/Application/Features/StockBatches/Models/StockBatchRequests.cs. */
export type GetAllStockBatchesRequest = BasePaginationFilter & {
  filters: ColumnFilter[];
};

/** Mirrors src/Application/Features/StockBatches/Models/StockBatchRequests.cs. */
export type CreateStockBatchRequest = {
  itemId: number;
  locationId: number;
  receivedClassId: number;
  quantity: number;
  expiryDate?: string | null;
  receivedDate: string;
  unitPrice: number;
};

export type StockBatchTableColumn =
  | 'itemName'
  | 'locationName'
  | 'quantity'
  | 'unitPrice'
  | 'lineTotal'
  | 'expiryDate'
  | 'receivedDate'
  | 'createdDate';

export const STOCK_BATCH_TABLE_COLUMNS: TableColumnDefinition<StockBatchTableColumn> = {
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
  quantity: {
    label: 'Cantitate',
    value: 'quantity',
    fieldType: 'number'
  },
  unitPrice: {
    label: 'Pret unitar',
    value: 'unitPrice',
    fieldType: 'number'
  },
  lineTotal: {
    label: 'Valoare totala',
    value: 'lineTotal',
    fieldType: 'number'
  },
  expiryDate: {
    label: 'Data expirare',
    value: 'expiryDate',
    fieldType: 'date'
  },
  receivedDate: {
    label: 'Data receptie',
    value: 'receivedDate',
    fieldType: 'date'
  },
  createdDate: {
    label: 'Data creare',
    value: 'createdDate',
    fieldType: 'date'
  }
};

export function buildStockBatchListFilter(
  currentFilter: GetAllStockBatchesRequest,
  partialFilter: Partial<GetAllStockBatchesRequest>
): GetAllStockBatchesRequest {
  return {
    ...currentFilter,
    ...partialFilter,
    cursor: null,
    pageSize: partialFilter.pageSize ?? currentFilter.pageSize ?? PAGINATION_PAGE_SIZE,
    sort: prioritizeSort(currentFilter.sort ?? [], partialFilter.sort ?? [])
  };
}
