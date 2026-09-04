import { BasePaginationFilter, ColumnFilter, prioritizeSort, TableColumnDefinition } from './pagination';
import { PAGINATION_PAGE_SIZE } from './category';
import { GetAllSchoolClassesRequest } from './school-class';

/** Mirrors src/Application/Features/GoodsReceipts/Models/GoodsReceiptDto.cs. */
export type GoodsReceiptLineDto = {
  stockBatchId: string;
  itemId: string;
  itemName: string;
  locationId: string;
  locationName: string;
  quantity: number;
  expiryDate?: string | null;
};

/** Mirrors src/Application/Features/GoodsReceipts/Models/GoodsReceiptDto.cs. */
export type GoodsReceiptDto = {
  id: string;
  classId: string;
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
  id: string;
  classId: string;
  className: string;
  receivedAt: string;
  supplierReference?: string | null;
  note: string;
  lineCount: number;
  totalQuantity: number;
  createdByName?: string | null;
  createdDate: string;
  totalAmount: number;
};

/** Mirrors src/Application/Features/GoodsReceipts/Models/GoodsReceiptRequests.cs. */
export type GetAllGoodsReceiptsRequest = BasePaginationFilter & {
  filters: ColumnFilter[];
};

/** Mirrors src/Application/Features/GoodsReceipts/Models/GoodsReceiptRequests.cs. */
export type CreateGoodsReceiptLineRequest = {
  itemId: string;
  locationId: string;
  quantity: number;
  expiryDate?: string | null;
};

/** Mirrors src/Application/Features/GoodsReceipts/Models/GoodsReceiptRequests.cs. */
export type CreateGoodsReceiptRequest = {
  classId: string;
  supplierReference?: string | null;
  note: string;
  lines: CreateGoodsReceiptLineRequest[];
};
/** Mirrors src/Domain/Enums/GoodsReceiptImportStatus.cs. */
export type GoodsReceiptImportStatus = 'processing' | 'pendingReview' | 'confirmed' | 'failed';

/** Mirrors src/Application/Features/GoodsReceipts/Models/GoodsReceiptImportDto.cs. */
export type GoodsReceiptImportDto = {
  id: string;
  status: GoodsReceiptImportStatus;
  classId: string;
  fileMetadataId: string;
  blobPath: string;
  uploadedAt: string;
};

/** Mirrors src/Application/Features/GoodsReceipts/Models/GoodsReceiptRequests.cs. */
export type CreateGoodsReceiptImportRequest = {
  classId: string;
  fileMetadataId: string;
};

/**
 * Row shape for the paginated goods-receipt-import list. Mirrors
 * src/Application/Features/GoodsReceipts/Models/GoodsReceiptImportListItemDto.cs.
 */
export type GoodsReceiptImportListItemDto = {
  id: string;
  status: GoodsReceiptImportStatus;
  classId: string;
  className: string;
  fileMetadataId: string;
  blobPath: string;
  errorMessage?: string | null;
  uploadedByName?: string | null;
  uploadedAt: string;
  processedAt?: string | null;
  createdDate: string;
};

/** Mirrors src/Application/Features/GoodsReceipts/Models/GoodsReceiptRequests.cs. */
export type GetAllGoodsReceiptImportsRequest = BasePaginationFilter & {
  filters: ColumnFilter[];
};

export type GoodsReceiptImportTableColumn =
  | 'className'
  | 'status'
  | 'uploadedByName'
  | 'uploadedAt'
  | 'processedAt'
  | 'errorMessage'
  | 'createdDate';

export const GOODS_RECEIPT_IMPORT_TABLE_COLUMNS: TableColumnDefinition<GoodsReceiptImportTableColumn> = {
  className: {
    label: 'Clasa',
    value: 'className',
    fieldType: 'string'
  },
  status: {
    label: 'Status',
    value: 'status',
    fieldType: 'select'
  },
  uploadedByName: {
    label: 'Incarcat de',
    value: 'uploadedByName',
    fieldType: 'string'
  },
  uploadedAt: {
    label: 'Data incarcarii',
    value: 'uploadedAt',
    fieldType: 'date'
  },
  processedAt: {
    label: 'Data procesarii',
    value: 'processedAt',
    fieldType: 'date'
  },
  errorMessage: {
    label: 'Eroare',
    value: 'errorMessage',
    fieldType: 'string'
  },
  createdDate: {
    label: 'Data creare',
    value: 'createdDate',
    fieldType: 'date'
  }
};

export function buildGoodsReceiptImportListFilter(
  currentFilter: GetAllGoodsReceiptImportsRequest,
  partialFilter: Partial<GetAllGoodsReceiptImportsRequest>
): GetAllGoodsReceiptImportsRequest {
  return {
    ...currentFilter,
    ...partialFilter,
    cursor: null,
    pageSize: partialFilter.pageSize ?? currentFilter.pageSize ?? PAGINATION_PAGE_SIZE,
    sort: prioritizeSort(currentFilter.sort ?? [], partialFilter.sort ?? [])
  };
}

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

export const GOODS_RECEIPT_IMPORT_STATUS_LABELS: Record<GoodsReceiptImportStatus, string> = {
  processing: 'In procesare',
  pendingReview: 'In asteptare revizuire',
  confirmed: 'Confirmat',
  failed: 'Eroare'
};

export const GOODS_RECEIPT_IMPORT_STATUS_COLORS: Record<GoodsReceiptImportStatus, string> = {
  processing: 'blue',
  pendingReview: 'orange',
  confirmed: 'green',
  failed: 'red'
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
