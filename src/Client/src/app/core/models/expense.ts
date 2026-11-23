import { BasePaginationFilter, ColumnFilter, prioritizeSort } from './pagination';
import { PAGINATION_PAGE_SIZE } from './category';

/** Mirrors src/Domain/Enums/ExpenseStatus.cs (serialized by name, camelCase). */
export type ExpenseStatus = 'processing' | 'processed' | 'failed';

/** Mirrors src/Domain/Enums/ExpenseSource.cs. */
export type ExpenseSource = 'manual' | 'receipt' | 'invoice';

/** Mirrors src/Application/Features/Expenses/Models/ExpenseDto.cs. */
export type ExpenseDto = {
  id: string;
  schoolClassId: string;
  source: ExpenseSource;
  status: ExpenseStatus;
  errorMessage?: string | null;
  supplierName?: string | null;
  documentNumber?: string | null;
  expenseDate?: string | null;
  currency: string;
  totalAmount?: number | null;
  vatAmount?: number | null;
  expenseCategoryId?: string | null;
  expenseCategoryName?: string | null;
  fileMetadataId?: string | null;
  uploadedAt: string;
  processedAt?: string | null;
  createdDate: string;
  lastModifiedDate: string;
};

/** Mirrors src/Application/Features/Expenses/Models/ExpenseRequests.cs. */
export type GetAllExpensesRequest = BasePaginationFilter & {
  filters: ColumnFilter[];
};

export type CreateExpenseFromDocumentRequest = {
  classId: string;
  fileMetadataId: string;
};

export type ExpenseFieldsRequest = {
  supplierName: string | null;
  documentNumber: string | null;
  expenseDate: string | null;
  currency: string | null;
  totalAmount: number | null;
  vatAmount: number | null;
  expenseCategoryId: string | null;
};

export type CreateExpenseRequest = {
  classId: string;
  fields: ExpenseFieldsRequest;
};

export function buildExpenseListFilter(
  currentFilter: GetAllExpensesRequest,
  partialFilter: Partial<GetAllExpensesRequest>
): GetAllExpensesRequest {
  return {
    ...currentFilter,
    ...partialFilter,
    cursor: null,
    pageSize: partialFilter.pageSize ?? currentFilter.pageSize ?? PAGINATION_PAGE_SIZE,
    sort: prioritizeSort(currentFilter.sort ?? [], partialFilter.sort ?? [])
  };
}

export const EXPENSE_STATUS_LABELS: Record<ExpenseStatus, string> = {
  processing: 'În procesare',
  processed: 'Procesat',
  failed: 'Eroare'
};

export const EXPENSE_STATUS_COLORS: Record<ExpenseStatus, string> = {
  processing: 'blue',
  processed: 'green',
  failed: 'red'
};

export const EXPENSE_SOURCE_LABELS: Record<ExpenseSource, string> = {
  manual: 'Manuală',
  receipt: 'Bon',
  invoice: 'Factură'
};
