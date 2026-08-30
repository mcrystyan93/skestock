import { BasePaginationFilter, ColumnFilter, prioritizeSort, TableColumnDefinition } from './pagination';

/** Mirrors src/Application/Features/Categories/Models/CategoryDto.cs. */
export type CategoryDto = {
  id: number;
  name: string;
  createdByName?: string | null;
  lastModifiedByName?: string | null;
  createdDate: string;
  lastModifiedDate: string;
};

/** Mirrors src/Application/Features/Categories/Models/CategoryRequests.cs. */
export type GetAllCategoriesRequest = BasePaginationFilter & {
  filters: ColumnFilter[];
};

/** Mirrors src/Application/Features/Categories/Models/CategoryRequests.cs. */
export type CreateCategoryRequest = {
  name: string;
};

/** Mirrors src/Application/Features/Categories/Models/CategoryRequests.cs. */
export type UpdateCategoryRequest = {
  name: string;
};
export const PAGINATION_PAGE_SIZE = 50;

export function buildCategoryListFilter(
  currentFilter: GetAllCategoriesRequest,
  partialFilter: Partial<GetAllCategoriesRequest>
): GetAllCategoriesRequest {
  return {
    ...currentFilter,
    ...partialFilter,
    cursor: null,
    pageSize: partialFilter.pageSize ?? currentFilter.pageSize ?? PAGINATION_PAGE_SIZE,
    sort: prioritizeSort(currentFilter.sort ?? [], partialFilter.sort ?? [])
  };
}

export type CategoryTableColumn =
  | 'name'
  | 'createdDate'
  | 'lastModifiedDate'
  | 'createdByName'
  | 'lastModifiedByName';

export const CATEGORY_TABLE_COLUMNS: TableColumnDefinition<CategoryTableColumn> = {
  name: {
    label: 'Nume',
    value: 'name',
    fieldType: 'string'
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

export type CategoryDropdownValue = CategoryDto | Partial<CategoryDto> | null;