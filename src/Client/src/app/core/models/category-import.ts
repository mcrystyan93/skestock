import { BasePaginationFilter, ColumnFilter, prioritizeSort, TableColumnDefinition } from './pagination';
import type { CategoryDto } from './category';

export type CategoryImportStatus = 'processing' | 'pendingReview' | 'confirmed' | 'failed';

export type CreateCategoryImportRequest = {
  fileMetadataId: string;
};

export type CategoryImportDto = {
  id: string;
  fileMetadataId: string;
  status: CategoryImportStatus;
  uploadedAt: string;
  processedAt?: string | null;
  errorMessage?: string | null;
};

export type CategoryImportListItemDto = {
  id: string;
  status: CategoryImportStatus;
  fileMetadataId: string;
  blobPath: string;
  errorMessage?: string | null;
  uploadedByName?: string | null;
  uploadedAt: string;
  processedAt?: string | null;
  createdDate: string;
};

export type GetAllCategoryImportsRequest = BasePaginationFilter & {
  filters: ColumnFilter[];
};

export function buildCategoryImportListFilter(
  currentFilter: GetAllCategoryImportsRequest,
  partialFilter: Partial<GetAllCategoryImportsRequest>
): GetAllCategoryImportsRequest {
  return {
    ...currentFilter,
    ...partialFilter,
    cursor: null,
    pageSize: partialFilter.pageSize ?? currentFilter.pageSize,
    sort: prioritizeSort(currentFilter.sort ?? [], partialFilter.sort ?? [])
  };
}

export type CategoryImportTableColumn =
  | 'blobPath'
  | 'status'
  | 'uploadedByName'
  | 'uploadedAt'
  | 'processedAt'
  | 'errorMessage'
  | 'createdDate';

export const CATEGORY_IMPORT_TABLE_COLUMNS: TableColumnDefinition<CategoryImportTableColumn> = {
  blobPath: {
    label: 'Fisier',
    value: 'blobPath',
    fieldType: 'string'
  },
  status: {
    label: 'Stare',
    value: 'status',
    fieldType: 'string'
  },
  uploadedByName: {
    label: 'Incarcat de',
    value: 'uploadedByName',
    fieldType: 'string'
  },
  uploadedAt: {
    label: 'Data incarcare',
    value: 'uploadedAt',
    fieldType: 'date'
  },
  processedAt: {
    label: 'Data procesare',
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

export const CATEGORY_IMPORT_STATUS_LABELS: Record<CategoryImportStatus, string> = {
  processing: 'Se proceseaza',
  pendingReview: 'In asteptarea revizuirii',
  confirmed: 'Confirmat',
  failed: 'Esuat'
};

export const CATEGORY_IMPORT_STATUS_COLORS: Record<CategoryImportStatus, string> = {
  processing: 'processing',
  pendingReview: 'orange',
  confirmed: 'success',
  failed: 'error'
};

export type CategoryImportSuggestionDto = {
  name: string;
  alreadyExists: boolean;
};

export type CategoryImportReviewDto = {
  id: string;
  status: CategoryImportStatus;
  errorMessage?: string | null;
  uploadedAt: string;
  processedAt?: string | null;
  suggestions: CategoryImportSuggestionDto[];
};

export type ConfirmCategoryImportRequest = {
  names: string[];
};

export type ConfirmCategoryImportResponse = {
  importId: string;
  status: CategoryImportStatus;
  categories: Array<{
    id: string;
    name: string;
    created: boolean;
  }>;
};
