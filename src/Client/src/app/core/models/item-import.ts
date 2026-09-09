import { BasePaginationFilter, ColumnFilter, prioritizeSort, TableColumnDefinition } from './pagination';

export type ItemImportStatus = 'processing' | 'pendingReview' | 'confirmed' | 'failed';

export type CreateItemImportRequest = {
  fileMetadataId: string;
};

export type ItemImportDto = {
  id: string;
  status: ItemImportStatus;
  fileMetadataId: string;
  blobPath: string;
  uploadedAt: string;
  processedAt?: string | null;
  errorMessage?: string | null;
};

export type ItemImportListItemDto = {
  id: string;
  status: ItemImportStatus;
  fileMetadataId: string;
  blobPath: string;
  errorMessage?: string | null;
  uploadedByName?: string | null;
  uploadedAt: string;
  processedAt?: string | null;
  createdDate: string;
};

export type GetAllItemImportsRequest = BasePaginationFilter & {
  filters: ColumnFilter[];
};

export function buildItemImportListFilter(
  currentFilter: GetAllItemImportsRequest,
  partialFilter: Partial<GetAllItemImportsRequest>
): GetAllItemImportsRequest {
  return {
    ...currentFilter,
    ...partialFilter,
    cursor: null,
    pageSize: partialFilter.pageSize ?? currentFilter.pageSize,
    sort: prioritizeSort(currentFilter.sort ?? [], partialFilter.sort ?? [])
  };
}

export type ItemImportTableColumn =
  | 'blobPath'
  | 'status'
  | 'uploadedByName'
  | 'uploadedAt'
  | 'processedAt'
  | 'errorMessage'
  | 'createdDate';

export const ITEM_IMPORT_TABLE_COLUMNS: TableColumnDefinition<ItemImportTableColumn> = {
  blobPath: { label: 'Fisier', value: 'blobPath', fieldType: 'string' },
  status: { label: 'Stare', value: 'status', fieldType: 'string' },
  uploadedByName: { label: 'Incarcat de', value: 'uploadedByName', fieldType: 'string' },
  uploadedAt: { label: 'Data incarcare', value: 'uploadedAt', fieldType: 'date' },
  processedAt: { label: 'Data procesare', value: 'processedAt', fieldType: 'date' },
  errorMessage: { label: 'Eroare', value: 'errorMessage', fieldType: 'string' },
  createdDate: { label: 'Data creare', value: 'createdDate', fieldType: 'date' }
};

export const ITEM_IMPORT_STATUS_LABELS: Record<ItemImportStatus, string> = {
  processing: 'Se proceseaza',
  pendingReview: 'In asteptarea revizuirii',
  confirmed: 'Confirmat',
  failed: 'Esuat'
};

export const ITEM_IMPORT_STATUS_COLORS: Record<ItemImportStatus, string> = {
  processing: 'processing',
  pendingReview: 'orange',
  confirmed: 'success',
  failed: 'error'
};

export type ItemImportReviewLineDto = {
  sku?: string | null;
  name: string;
  categoryName: string;
  unit: string;
  description?: string | null;
  isPerishable: boolean;
  itemAlreadyExists: boolean;
  categoryAlreadyExists: boolean;
};

export type ItemImportReviewDto = {
  id: string;
  status: ItemImportStatus;
  errorMessage?: string | null;
  uploadedAt: string;
  processedAt?: string | null;
  suggestions: ItemImportReviewLineDto[];
};

export type ConfirmItemImportRequestItem = ItemImportReviewLineDto;

export type ConfirmItemImportRequest = {
  items: ConfirmItemImportRequestItem[];
};

export type ItemImportConfirmationResultDto = {
  importId: string;
  status: ItemImportStatus;
  items: Array<{
    id: string;
    sku?: string | null;
    name: string;
    categoryName: string;
    created: boolean;
  }>;
};
