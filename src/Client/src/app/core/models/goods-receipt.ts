import { BasePaginationFilter, ColumnFilter } from './pagination';

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
