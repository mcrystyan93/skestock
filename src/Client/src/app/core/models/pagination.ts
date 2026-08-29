import { NzTableSortOrder } from 'ng-zorro-antd/table';
import { filter, find, isNil, remove, some } from 'lodash-es';

/**
 * Mirrors src/Application/Common/Filtering/FilterOperator.cs. System.Text.Json has no
 * JsonStringEnumConverter registered for this API, so enums serialize as their ordinal
 * (numeric) value on the wire — values here must stay in the same declaration order as
 * the C# enum.
 */
// export enum FilterOperator {
//   Equals = 0,
//   NotEquals = 1,
//   Contains = 2,
//   GreaterThan = 3,
//   LessThan = 4,
//   In = 5,
//   Between = 6,
// }
export type FilterOperator =
  'equals' | 'notEquals' | 'contains' | 'greaterThan' | 'lessThan' | 'in' | 'between';

export type FieldType = 'string' | 'number' | 'date' | 'select' | 'boolean';
export type SelectType = 'user' | 'department';

/** Mirrors src/Application/Common/Filtering/ColumnFilter.cs. */
export type ColumnFilter = {
  field: string;
  operator: FilterOperator;
  value: unknown;
  displayValue?: string;
  fieldType: FieldType;
  selectType?: SelectType;
  booleanDisplaySelector?: {
    [key: string]: string
  };
};
export type DisplayColumnFilter<T> = ColumnFilter & {
  fieldDescription: {
    label: string;
    value: T;
  };
  operatorDescription: {
    label: string;
    value: FilterOperator;
  };
};
export type ColumnDefinitionData<T extends string> = {
  label: string;
  value: T;
  fieldType: FieldType;
  selectType?: SelectType;
  booleanDisplaySelector?: {
    [key: string]: string
  };
};
export type TableColumnDefinition<T extends string> = Record<T, ColumnDefinitionData<T>>;
export type OperatorItem = {
  label: string;
  value: FilterOperator;
};
export const OPERATORS_BY_TYPE: Record<FieldType, OperatorItem[]> = {
  string: [
    {label: 'equals', value: 'equals'},
    {label: 'notEquals', value: 'notEquals'},
    {label: 'contains', value: 'contains'},
  ],
  number: [
    {label: 'equals', value: 'equals'},
    {label: 'greaterThan', value: 'greaterThan'},
    {label: 'lessThan', value: 'lessThan'},
    {label: 'between', value: 'between'},
  ],
  date: [
    {label: 'equals', value: 'equals'},
    {label: 'before', value: 'lessThan'},
    {label: 'after', value: 'greaterThan'},
    {label: 'between', value: 'between'},
  ],
  select: [
    {label: 'equals', value: 'equals'},
    {label: 'notEquals', value: 'notEquals'},
  ],
  boolean: [
    {label: 'equals', value: 'equals'}
  ]
};

/** Mirrors src/Application/Common/Models/PaginationSort.cs. */
export type PaginationSort = {
  key: string;
  value: NzTableSortOrder;
};

/** Mirrors src/Application/Common/Models/BasePaginationFilter.cs. */
export type BasePaginationFilter = {
  searchTerm?: string | null;
  pageSize?: number;
  /** Opaque keyset pagination cursor (base64-encoded JSON with sort values and position). */
  cursor?: string | null;
  sort?: PaginationSort[];
};
export type PaginatedResponseData = {
  /** Opaque cursor for fetching the next page. Pass as `cursor` to fetch the next page. */
  nextCursor?: string | null;
  /** Indicates whether more records exist after this page. */
  hasNextPage: boolean;
  sort: PaginationSort[];
};
/** Mirrors src/Application/Common/Models/PaginatedResponse.cs. */
export type PaginatedResponse<T> = PaginatedResponseData & {
  data: T[];
};

export const prioritizeSort = (
  currentSort: Array<PaginationSort>,
  newSort: Array<PaginationSort>,
) => {
  for (const item of currentSort) {
    const existing = find(newSort, (i) => i.key === item.key);

    if (isNil(existing)) continue;

    if (existing.value === item.value) continue;
    // update the order in case it changed
    item.value = existing.value;
  }

  // remove those that have been removed
  remove(currentSort, (item) => !some(newSort, (ii) => item.key === ii.key));
  // add those that were newly added
  currentSort.push(...filter(newSort, (item) => !some(currentSort, (ii) => item.key === ii.key)));

  return currentSort;
};
