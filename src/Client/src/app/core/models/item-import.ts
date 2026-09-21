import { BasePaginationFilter, ColumnFilter, prioritizeSort, TableColumnDefinition } from './pagination';
import type { CategoryDropdownValue } from './category';
import type { ItemDropdownValue } from './item';
import type { ImportBatchFileDto, ImportBatchHistoryDto, ImportBatchStatus } from './import-batch';

export type CreateItemImportBatchRequest = {
  fileMetadataIds: string[];
  clientRequestId?: string;
};

export type ItemImportBatchFileDto = ImportBatchFileDto;
export type ItemImportBatchStatus = ImportBatchStatus;

export type ItemImportBatchDto = {
  id: string;
  status: ItemImportBatchStatus;
  clientRequestId?: string | null;
  attemptCount: number;
  files: ItemImportBatchFileDto[];
  history: ImportBatchHistoryDto[];
  uploadedAt: string;
  processedAt?: string | null;
  errorMessage?: string | null;
};

export type ItemImportBatchListItemDto = {
  id: string;
  status: ItemImportBatchStatus;
  files: ItemImportBatchFileDto[];
  errorMessage?: string | null;
  uploadedByName?: string | null;
  uploadedAt: string;
  processedAt?: string | null;
  createdDate: string;
};

export type GetAllItemImportBatchesRequest = BasePaginationFilter & {
  filters: ColumnFilter[];
};

export function buildItemImportBatchListFilter(
  currentFilter: GetAllItemImportBatchesRequest,
  partialFilter: Partial<GetAllItemImportBatchesRequest>
): GetAllItemImportBatchesRequest {
  return {
    ...currentFilter,
    ...partialFilter,
    cursor: null,
    pageSize: partialFilter.pageSize ?? currentFilter.pageSize,
    sort: prioritizeSort(currentFilter.sort ?? [], partialFilter.sort ?? [])
  };
}

export type ItemImportBatchTableColumn =
  | 'files'
  | 'status'
  | 'uploadedByName'
  | 'uploadedAt'
  | 'processedAt'
  | 'errorMessage'
  | 'createdDate';

export const ITEM_IMPORT_BATCH_TABLE_COLUMNS: TableColumnDefinition<ItemImportBatchTableColumn> = {
  files: { label: 'Fisiere', value: 'files', fieldType: 'string' },
  status: { label: 'Stare', value: 'status', fieldType: 'string' },
  uploadedByName: { label: 'Incarcat de', value: 'uploadedByName', fieldType: 'string' },
  uploadedAt: { label: 'Incarcat', value: 'uploadedAt', fieldType: 'date' },
  processedAt: { label: 'Data procesare', value: 'processedAt', fieldType: 'date' },
  errorMessage: { label: 'Eroare', value: 'errorMessage', fieldType: 'string' },
  createdDate: { label: 'Data creare', value: 'createdDate', fieldType: 'date' }
};

export const ITEM_IMPORT_BATCH_STATUS_LABELS: Record<ItemImportBatchStatus, string> = {
  all: 'Toate',
  processing: 'Se proceseaza',
  pendingReview: 'In asteptare',
  confirmed: 'Confirmat',
  failed: 'Esuat'
};

export const ITEM_IMPORT_BATCH_STATUS_COLORS: Record<ItemImportBatchStatus, string> = {
  all: 'default',
  processing: 'processing',
  pendingReview: 'orange',
  confirmed: 'success',
  failed: 'error'
};

export type ItemImportReviewLineDto = {
  sku: string;
  name: string;
  categoryName: string;
  unit: string;
  description: string;
  isPerishable: boolean;
  itemAlreadyExists: boolean;
  categoryAlreadyExists: boolean;
  matchedCategory?: CategoryDropdownValue;
  matchedItem?: ItemDropdownValue;
};

export type ItemImportBatchReviewDto = {
  id: string;
  status: ItemImportBatchStatus;
  clientRequestId?: string | null;
  attemptCount: number;
  errorMessage?: string | null;
  uploadedAt: string;
  processedAt?: string | null;
  files: ItemImportBatchFileDto[];
  history: ImportBatchHistoryDto[];
  suggestions: ItemImportReviewLineDto[];
};

export type ConfirmItemImportRequestItem = Omit<
  ItemImportReviewLineDto,
  'categoryName' | 'matchedCategory' | 'matchedItem' | 'categoryAlreadyExists' | 'itemAlreadyExists'
> & {
  itemId: string;
};

export type ConfirmItemImportBatchRequest = {
  items: ConfirmItemImportRequestItem[];
};

export type ConfirmItemImportBatchResponse = {
  batchId: string;
  status: ItemImportBatchStatus;
  items: Array<{
    id: string;
    sku?: string | null;
    name: string;
    categoryName: string;
    created: boolean;
    categoryCreated: boolean;
  }>;
};
