import { BasePaginationFilter, ColumnFilter, prioritizeSort, TableColumnDefinition } from './pagination';
import { PAGINATION_PAGE_SIZE } from './category';

/** Mirrors src/Application/Features/Items/Models/ItemDto.cs. */
export type ItemDto = {
  id: string;
  sku?: string | null;
  name: string;
  description?: string | null;
  unit: string;
  minThreshold: number;
  isPerishable: boolean;
  shelfLifeDays?: number | null;
  isActive: boolean;
  categoryId: string;
  categoryName?: string | null;
  createdByName?: string | null;
  lastModifiedByName?: string | null;
  createdDate: string;
  lastModifiedDate: string;
};

/** Mirrors src/Application/Features/Items/Models/ItemRequests.cs. */
export type GetAllItemsRequest = BasePaginationFilter & {
  filters: ColumnFilter[];
};

/** Mirrors src/Application/Features/Items/Models/ItemRequests.cs. */
export type CreateItemRequest = {
  sku?: string | null;
  name: string;
  description?: string | null;
  unit: string;
  minThreshold: number;
  isPerishable: boolean;
  shelfLifeDays?: number | null;
  categoryId: string;
};

/** Mirrors src/Application/Features/Items/Models/ItemRequests.cs. */
export type EditItemRequest = {
  sku?: string | null;
  name: string;
  description?: string | null;
  unit: string;
  minThreshold: number;
  isPerishable: boolean;
  shelfLifeDays?: number | null;
  categoryId: string;
};

export type ItemTableColumn =
  | 'sku'
  | 'name'
  | 'unit'
  | 'minThreshold'
  | 'isPerishable'
  | 'shelfLifeDays'
  | 'categoryName'
  | 'isActive'
  | 'createdDate'
  | 'lastModifiedDate'
  | 'createdByName'
  | 'lastModifiedByName';

export const ITEM_TABLE_COLUMNS: TableColumnDefinition<ItemTableColumn> = {
  sku: {
    label: 'Cod',
    value: 'sku',
    fieldType: 'string'
  },
  name: {
    label: 'Nume',
    value: 'name',
    fieldType: 'string'
  },
  unit: {
    label: 'Unitate',
    value: 'unit',
    fieldType: 'string'
  },
  minThreshold: {
    label: 'Prag minim',
    value: 'minThreshold',
    fieldType: 'number'
  },
  isPerishable: {
    label: 'Perisabil',
    value: 'isPerishable',
    fieldType: 'boolean'
  },
  shelfLifeDays: {
    label: 'Valabilitate (zile)',
    value: 'shelfLifeDays',
    fieldType: 'number'
  },
  categoryName: {
    label: 'Categorie',
    value: 'categoryName',
    fieldType: 'string'
  },
  isActive: {
    label: 'Activ',
    value: 'isActive',
    fieldType: 'boolean'
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

export type ItemDropdownOption = Partial<ItemDto> & Pick<ItemDto, 'id'>;

export type ItemDropdownValue = ItemDto | ItemDropdownOption | null;

export function buildItemListFilter(
  currentFilter: GetAllItemsRequest,
  partialFilter: Partial<GetAllItemsRequest>
): GetAllItemsRequest {
  return {
    ...currentFilter,
    ...partialFilter,
    cursor: null,
    pageSize: partialFilter.pageSize ?? currentFilter.pageSize ?? PAGINATION_PAGE_SIZE,
    sort: prioritizeSort(currentFilter.sort ?? [], partialFilter.sort ?? [])
  };
}
