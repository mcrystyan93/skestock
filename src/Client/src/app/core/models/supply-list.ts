import { BasePaginationFilter, ColumnFilter, prioritizeSort, TableColumnDefinition } from './pagination';
import { PAGINATION_PAGE_SIZE } from './category';

/** Mirrors src/Domain/Enums/SupplyListFrequency.cs. */
export type SupplyListFrequency =
  | 'Weekly'
  | 'EveryXWeeks'
  | 'Monthly'
  | 'StartOfSchoolYear'
  | 'EndOfSchoolYear'
  | 'MiddleOfSemester'
  | 'StartOfMonth'
  | 'EndOfMonth'
  | 'Once';

export const SUPPLY_LIST_FREQUENCY_LABELS: Record<SupplyListFrequency, string> = {
  Weekly: 'Săptămânal',
  EveryXWeeks: 'La fiecare X săptămâni',
  Monthly: 'Lunar',
  StartOfSchoolYear: 'La începutul clasei',
  EndOfSchoolYear: 'La sfârșitul clasei',
  MiddleOfSemester: 'Mijlocul clasei',
  StartOfMonth: 'La începutul lunii',
  EndOfMonth: 'La sfârșitul lunii',
  Once: 'O singură dată'
};

export const SUPPLY_LIST_FREQUENCY_OPTIONS: { label: string; value: SupplyListFrequency }[] =
  (Object.keys(SUPPLY_LIST_FREQUENCY_LABELS) as SupplyListFrequency[])
    .map((value) => ({ value, label: SUPPLY_LIST_FREQUENCY_LABELS[value] }));

export const SUPPLY_LIST_MIN_INTERVAL_WEEKS = 2;
export const SUPPLY_LIST_MAX_INTERVAL_WEEKS = 52;

export function formatSupplyListFrequency(
  frequency: SupplyListFrequency | string,
  intervalWeeks?: number | null
): string {
  if (frequency === 'EveryXWeeks')
    return intervalWeeks ? `La fiecare ${intervalWeeks} săptămâni` : SUPPLY_LIST_FREQUENCY_LABELS.EveryXWeeks;

  return SUPPLY_LIST_FREQUENCY_LABELS[frequency as SupplyListFrequency] ?? frequency;
}

/** Mirrors src/Application/Features/SupplyLists/Models/SupplyListLineDto.cs. */
export type SupplyListLineDto = {
  id: string;
  itemId: string;
  itemName: string;
  itemSku?: string | null;
  categoryName?: string | null;
  quantity: number;
  unit: string;
  notes?: string | null;
};

/** Mirrors src/Application/Features/SupplyLists/Models/SupplyListDto.cs. */
export type SupplyListDto = {
  id: string;
  name: string;
  note?: string | null;
  frequency: SupplyListFrequency;
  intervalWeeks?: number | null;
  isActive: boolean;
  lines: SupplyListLineDto[];
  createdByName?: string | null;
  lastModifiedByName?: string | null;
  createdDate: string;
  lastModifiedDate: string;
};

/** Mirrors src/Application/Features/SupplyLists/Models/SupplyListListItemDto.cs. */
export type SupplyListListItemDto = {
  id: string;
  name: string;
  frequency: SupplyListFrequency;
  intervalWeeks?: number | null;
  isActive: boolean;
  lineCount: number;
  createdDate: string;
  lastModifiedDate: string;
};

/** Mirrors src/Application/Features/SupplyLists/Models/SupplyListRequests.cs. */
export type GetAllSupplyListsRequest = BasePaginationFilter & {
  filters: ColumnFilter[];
};

/** Mirrors src/Application/Features/SupplyLists/Models/SupplyListRequests.cs. */
export type SupplyListLineRequest = {
  itemId: string;
  quantity: number;
  unit?: string | null;
  notes?: string | null;
};

/** Mirrors src/Application/Features/SupplyLists/Models/SupplyListRequests.cs. */
export type SaveSupplyListRequest = {
  name: string;
  note?: string | null;
  frequency: SupplyListFrequency;
  intervalWeeks?: number | null;
  lines: SupplyListLineRequest[];
};

export type CreateSupplyListRequest = SaveSupplyListRequest;
export type UpdateSupplyListRequest = SaveSupplyListRequest;

export type SupplyListActiveFilter = 'active' | 'inactive' | 'all';

export type SupplyListTableColumn =
  | 'name'
  | 'frequency'
  | 'lineCount'
  | 'isActive'
  | 'lastModifiedDate'
  | 'actions';

export const SUPPLY_LIST_TABLE_COLUMNS: TableColumnDefinition<SupplyListTableColumn> = {
  name: { label: 'Nume', value: 'name', fieldType: 'string' },
  frequency: { label: 'Frecvență', value: 'frequency', fieldType: 'select' },
  lineCount: { label: 'Nr. articole', value: 'lineCount', fieldType: 'number' },
  isActive: { label: 'Status', value: 'isActive', fieldType: 'boolean' },
  lastModifiedDate: { label: 'Ultima modificare', value: 'lastModifiedDate', fieldType: 'date' },
  actions: { label: 'Acțiuni', value: 'actions', fieldType: 'string' }
};

export function buildSupplyListFilter(
  currentFilter: GetAllSupplyListsRequest,
  partialFilter: Partial<GetAllSupplyListsRequest>
): GetAllSupplyListsRequest {
  return {
    ...currentFilter,
    ...partialFilter,
    cursor: null,
    pageSize: partialFilter.pageSize ?? currentFilter.pageSize ?? PAGINATION_PAGE_SIZE,
    sort: prioritizeSort(currentFilter.sort ?? [], partialFilter.sort ?? [])
  };
}

export const SUPPLY_LIST_ACTIVE_FILTER_FIELD = 'isActive';

export function buildSupplyListActiveFilter(active: SupplyListActiveFilter): ColumnFilter | null {
  if (active === 'all')
    return null;

  return {
    field: SUPPLY_LIST_ACTIVE_FILTER_FIELD,
    operator: 'equals',
    value: active === 'active',
    fieldType: 'boolean',
    displayValue: active === 'active' ? 'Active' : 'Inactive'
  };
}

export function getSupplyListActiveFilter(filters: ColumnFilter[]): SupplyListActiveFilter {
  const filter = filters.find((f) => f.field === SUPPLY_LIST_ACTIVE_FILTER_FIELD);

  if (!filter)
    return 'all';

  return filter.value === true || filter.value === 'true' ? 'active' : 'inactive';
}
