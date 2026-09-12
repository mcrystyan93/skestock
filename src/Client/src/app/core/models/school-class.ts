import { BasePaginationFilter, ColumnFilter, prioritizeSort, TableColumnDefinition } from './pagination';
import { PAGINATION_PAGE_SIZE } from './category';

/**
 * Mirrors src/Domain/Enums/ClassStatus.cs. No JsonStringEnumConverter is registered for this
 * API, so enums serialize as their ordinal (numeric) value on the wire — values here must stay
 * in the same declaration order as the C# enum.
 */
export enum ClassStatus {
  Upcoming = 0,
  Active = 1,
  Paused = 2,
  Closed = 3,
}

/** Mirrors src/Application/Features/SchoolClasses/Models/SchoolClassDto.cs. */
export type SchoolClassDto = {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  status: ClassStatus;
  createdByName?: string | null;
  lastModifiedByName?: string | null;
  createdDate: string;
  lastModifiedDate: string;
};

export type SchoolClassDropdownValue = SchoolClassDto | Partial<SchoolClassDto> | null;

/** Mirrors src/Application/Features/SchoolClasses/Models/SchoolClassDto.cs. */
export type SchoolClassSummary = {
  id: string;
  noOfGoodsReceipt: number;
  totalAmount: number;
  lowStockItemsCount: number;
  processingImportsCount: number;
  pendingReviewImportsCount: number;
  failedImportsCount: number;
};

/** Mirrors src/Application/Features/SchoolClasses/Models/SchoolClassRequests.cs. */
export type GetAllSchoolClassesRequest = BasePaginationFilter & {
  filters: ColumnFilter[];
};

/** Mirrors src/Application/Features/SchoolClasses/Models/SchoolClassRequests.cs. */
export type CreateSchoolClassRequest = {
  name: string;
  startDate: string;
  endDate: string;
  status: ClassStatus;
};

/** Mirrors src/Application/Features/SchoolClasses/Models/SchoolClassRequests.cs. */
export type UpdateSchoolClassRequest = {
  name: string;
  startDate: string;
  endDate: string;
  status: ClassStatus;
};

export type SchoolClassTableColumn =
  | 'name'
  | 'startDate'
  | 'endDate'
  | 'status'
  | 'createdDate'
  | 'lastModifiedDate'
  | 'createdByName'
  | 'lastModifiedByName';

export const SCHOOL_CLASS_TABLE_COLUMNS: TableColumnDefinition<SchoolClassTableColumn> = {
  name: {
    label: 'Nume',
    value: 'name',
    fieldType: 'string'
  },
  startDate: {
    label: 'Data început',
    value: 'startDate',
    fieldType: 'date'
  },
  endDate: {
    label: 'Data sfârșit',
    value: 'endDate',
    fieldType: 'date'
  },
  status: {
    label: 'Stare',
    value: 'status',
    fieldType: 'select'
  },
  createdDate: {
    label: 'Data creare',
    value: 'createdDate',
    fieldType: 'date'
  },
  lastModifiedDate: {
    label: 'Data modificare',
    value: 'lastModifiedDate',
    fieldType: 'date'
  },
  createdByName: {
    label: 'Creat de',
    value: 'createdByName',
    fieldType: 'string'
  },
  lastModifiedByName: {
    label: 'Modificat de',
    value: 'lastModifiedByName',
    fieldType: 'string'
  }
};

export const CLASS_STATUS_OPTIONS: Array<{ label: string; value: ClassStatus }> = [
  { label: 'Viitoare', value: ClassStatus.Upcoming },
  { label: 'Activă', value: ClassStatus.Active },
  { label: 'Suspendată', value: ClassStatus.Paused },
  { label: 'Închisă', value: ClassStatus.Closed }
];

export const CLASS_STATUS_LABELS: Record<ClassStatus, string> = {
  [ClassStatus.Upcoming]: 'Viitoare',
  [ClassStatus.Active]: 'Activă',
  [ClassStatus.Paused]: 'Suspendată',
  [ClassStatus.Closed]: 'Închisă'
};

export const CLASS_STATUS_COLORS: Record<ClassStatus, string> = {
  [ClassStatus.Upcoming]: 'blue',
  [ClassStatus.Active]: 'success',
  [ClassStatus.Paused]: 'orange',
  [ClassStatus.Closed]: 'red'
};

export function buildSchoolClassListFilter(
  currentFilter: GetAllSchoolClassesRequest,
  partialFilter: Partial<GetAllSchoolClassesRequest>
): GetAllSchoolClassesRequest {
  return {
    ...currentFilter,
    ...partialFilter,
    cursor: null,
    pageSize: partialFilter.pageSize ?? currentFilter.pageSize ?? PAGINATION_PAGE_SIZE,
    sort: prioritizeSort(currentFilter.sort ?? [], partialFilter.sort ?? [])
  };
}

export enum SchoolClassTab {
  Receipts = 0,
  Stock = 1,
  Imports = 2
};
