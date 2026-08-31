import { BasePaginationFilter, ColumnFilter, prioritizeSort, TableColumnDefinition } from './pagination';
import { PAGINATION_PAGE_SIZE } from './category';
import { GetAllSchoolClassesRequest } from './school-class';

/** Mirrors src/Application/Features/GoodsReceipts/Models/GoodsReceiptDto.cs. */
export type GoodsReceiptLineDto = {
  stockBatchId: number;
  itemId: number;
  itemName: string;
  locationId: number;
  locationName: string;
  quantity: number;
  expiryDate?: string | null;
};

/** Mirrors src/Application/Features/GoodsReceipts/Models/GoodsReceiptDto.cs. */
export type GoodsReceiptDto = {
  id: number;
  classId: number;
  className: string;
  receivedAt: string;
  supplierReference?: string | null;
  note: string;
  lines: GoodsReceiptLineDto[];
  createdByName?: string | null;
  lastModifiedByName?: string | null;
  createdDate: string;
  lastModifiedDate: string;
};

/**
 * Lightweight row shape for the paginated goods-receipt list - a line count/quantity summary
 * instead of the full line detail (see `GoodsReceiptDto`), mirrors
 * src/Application/Features/GoodsReceipts/Models/GoodsReceiptDto.cs.
 */
export type GoodsReceiptListItemDto = {
  id: number;
  classId: number;
  className: string;
  receivedAt: string;
  supplierReference?: string | null;
  note: string;
  lineCount: number;
  totalQuantity: number;
  createdByName?: string | null;
  createdDate: string;
  totalAmount:number;
};

/** Mirrors src/Application/Features/GoodsReceipts/Models/GoodsReceiptRequests.cs. */
export type GetAllGoodsReceiptsRequest = BasePaginationFilter & {
  filters: ColumnFilter[];
};

/** Mirrors src/Application/Features/GoodsReceipts/Models/GoodsReceiptRequests.cs. */
export type CreateGoodsReceiptLineRequest = {
  itemId: number;
  locationId: number;
  quantity: number;
  expiryDate?: string | null;
};

/** Mirrors src/Application/Features/GoodsReceipts/Models/GoodsReceiptRequests.cs. */
export type CreateGoodsReceiptRequest = {
  classId: number;
  supplierReference?: string | null;
  note: string;
  lines: CreateGoodsReceiptLineRequest[];
};
export type GoodsReceiptTableColumn =
  | 'className'
  | 'receivedAt'
  | 'supplierReference'
  | 'lineCount'
  | 'totalQuantity'
  | 'totalAmount'
  | 'createdByName'
  | 'createdDate';

export const GOODS_RECEIPT_TABLE_COLUMNS: TableColumnDefinition<GoodsReceiptTableColumn> = {
  className: {
    label: 'Clasa',
    value: 'className',
    fieldType: 'string'
  },
  receivedAt: {
    label: 'Data receptiei',
    value: 'receivedAt',
    fieldType: 'date'
  },
  supplierReference: {
    label: 'Referinta furnizor',
    value: 'supplierReference',
    fieldType: 'string'
  },
  lineCount: {
    label: 'Nr. articole',
    value: 'lineCount',
    fieldType: 'number'
  },
  totalQuantity: {
    label: 'Cantitate totala',
    value: 'totalQuantity',
    fieldType: 'number'
  },
  totalAmount: {
    label: 'Valoare totala',
    value: 'totalAmount',
    fieldType: 'number'
  },
  createdByName: {
    label: 'Creat de',
    value: 'createdByName',
    fieldType: 'string'
  },
  createdDate: {
    label: 'Data creare',
    value: 'createdDate',
    fieldType: 'date'
  }
};

export function buildGoodsReceiptListFilter(
  currentFilter: GetAllGoodsReceiptsRequest,
  partialFilter: Partial<GetAllGoodsReceiptsRequest>
): GetAllGoodsReceiptsRequest {
  return {
    ...currentFilter,
    ...partialFilter,
    cursor: null,
    pageSize: partialFilter.pageSize ?? currentFilter.pageSize ?? PAGINATION_PAGE_SIZE,
    sort: prioritizeSort(currentFilter.sort ?? [], partialFilter.sort ?? [])
  };
}
