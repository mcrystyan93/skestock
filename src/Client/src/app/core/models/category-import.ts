import { BasePaginationFilter, ColumnFilter, prioritizeSort, TableColumnDefinition } from './pagination';
import type { CategoryDropdownValue } from './category';
import type { ImportBatchFileDto, ImportBatchHistoryDto, ImportBatchStatus } from './import-batch';

export type CreateCategoryImportBatchRequest = {
  fileMetadataIds: string[];
  clientRequestId?: string;
};

export type CategoryImportBatchFileDto = ImportBatchFileDto;
export type CategoryImportBatchStatus = ImportBatchStatus;

export type CategoryImportBatchMutationDto = {
  id: string;
  status: CategoryImportBatchStatus;
};

export type CategoryImportBatchListItemDto = {
  id: string;
  status: CategoryImportBatchStatus;
  files: CategoryImportBatchFileDto[];
  errorMessage?: string | null;
  uploadedByName?: string | null;
  uploadedAt: string;
  processedAt?: string | null;
  createdDate: string;
};

export type GetAllCategoryImportBatchesRequest = BasePaginationFilter & {
  filters: ColumnFilter[];
};

export function buildCategoryImportBatchListFilter(
  currentFilter: GetAllCategoryImportBatchesRequest,
  partialFilter: Partial<GetAllCategoryImportBatchesRequest>
): GetAllCategoryImportBatchesRequest {
  return {
    ...currentFilter,
    ...partialFilter,
    cursor: null,
    pageSize: partialFilter.pageSize ?? currentFilter.pageSize,
    sort: prioritizeSort(currentFilter.sort ?? [], partialFilter.sort ?? [])
  };
}

export type CategoryImportBatchTableColumn =
  | 'files'
  | 'status'
  | 'uploadedByName'
  | 'uploadedAt'
  | 'processedAt'
  | 'errorMessage'
  | 'createdDate';

export const CATEGORY_IMPORT_BATCH_TABLE_COLUMNS: TableColumnDefinition<CategoryImportBatchTableColumn> = {
  files: { label: 'Fisiere', value: 'files', fieldType: 'string' },
  status: { label: 'Stare', value: 'status', fieldType: 'string' },
  uploadedByName: { label: 'Incarcat', value: 'uploadedByName', fieldType: 'string' },
  uploadedAt: { label: 'Incarcat', value: 'uploadedAt', fieldType: 'date' },
  processedAt: { label: 'Data procesare', value: 'processedAt', fieldType: 'date' },
  errorMessage: { label: 'Eroare', value: 'errorMessage', fieldType: 'string' },
  createdDate: { label: 'Data creare', value: 'createdDate', fieldType: 'date' }
};

export const CATEGORY_IMPORT_BATCH_STATUS_LABELS: Record<CategoryImportBatchStatus, string> = {
  all: 'Toate',
  processing: 'Se proceseaza',
  pendingReview: 'In asteptare',
  confirmed: 'Confirmat',
  failed: 'Esuat'
};

export const CATEGORY_IMPORT_BATCH_STATUS_COLORS: Record<CategoryImportBatchStatus, string> = {
  all: 'default',
  processing: 'processing',
  pendingReview: 'orange',
  confirmed: 'success',
  failed: 'error'
};

export type CategoryImportReviewLineDto = {
  name: string;
  alreadyExists: boolean;
  matchedCategory?: CategoryDropdownValue | null;
};

export type CategoryImportBatchReviewDto = {
  id: string;
  status: CategoryImportBatchStatus;
  clientRequestId?: string | null;
  attemptCount: number;
  errorMessage?: string | null;
  uploadedAt: string;
  processedAt?: string | null;
  files: CategoryImportBatchFileDto[];
  history: ImportBatchHistoryDto[];
  suggestions: CategoryImportReviewLineDto[];
};

export type ConfirmCategoryImportBatchRequest = {
  names: string[];
};

export type ConfirmCategoryImportBatchResponse = {
  batchId: string;
  status: CategoryImportBatchStatus;
};
