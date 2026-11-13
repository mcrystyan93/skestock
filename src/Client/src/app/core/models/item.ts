import { BasePaginationFilter, ColumnFilter, prioritizeSort, TableColumnDefinition } from './pagination';
import { PAGINATION_PAGE_SIZE } from './category';

/** Mirrors src/Application/Features/Items/Models/ItemLocationThresholdDto.cs. */
export type ItemLocationThreshold = {
  locationId: string;
  minThreshold: number;
};

/** Mirrors src/Application/Features/Items/Models/ItemDto.cs. */
export type ItemDto = {
  id: string;
  sku?: string | null;
  name: string;
  description?: string | null;
  unit: string;
  locationThresholds: ItemLocationThreshold[];
  isPerishable: boolean;
  shelfLifeDays?: number | null;
  isActive: boolean;
  categoryId: string;
  categoryName?: string | null;
  locationId?: string | null;
  imageFileId?: string | null;
  createdByName?: string | null;
  lastModifiedByName?: string | null;
  createdDate: string;
  lastModifiedDate: string;
};

/** Mirrors src/Application/Features/Items/Models/ItemRequests.cs. */
export type GetAllItemsRequest = BasePaginationFilter & {
  filters: ColumnFilter[];
};

export type ItemActiveFilter = 'active' | 'inactive' | 'all';

export const ITEM_ACTIVE_FILTER_FIELD = 'isActive';

export function buildItemActiveFilter(active: ItemActiveFilter): ColumnFilter | null {
  if (active === 'all')
    return null;

  return {
    field: ITEM_ACTIVE_FILTER_FIELD,
    operator: 'equals',
    value: active === 'active',
    fieldType: 'boolean',
    displayValue: active === 'active' ? 'Active' : 'Inactive'
  };
}

export function getItemActiveFilter(filters: ColumnFilter[]): ItemActiveFilter {
  const filter = filters.find((f) => f.field === ITEM_ACTIVE_FILTER_FIELD);

  if (!filter)
    return 'all';

  return filter.value === true || filter.value === 'true' ? 'active' : 'inactive';
}

/** Mirrors src/Application/Features/Items/Models/ItemRequests.cs. */
export type CreateItemRequest = {
  sku?: string | null;
  name: string;
  description?: string | null;
  unit: string;
  locationThresholds: ItemLocationThreshold[];
  isPerishable: boolean;
  shelfLifeDays?: number | null;
  categoryId: string;
  locationId?: string | null;
  imageFileId?: string | null;
};

/** Mirrors src/Application/Features/Items/Models/ItemRequests.cs. */
export type EditItemRequest = {
  sku?: string | null;
  name: string;
  description?: string | null;
  unit: string;
  locationThresholds: ItemLocationThreshold[];
  isPerishable: boolean;
  shelfLifeDays?: number | null;
  categoryId: string;
  locationId?: string | null;
  imageFileId?: string | null;
};

/** Mirrors src/Application/Features/Items/Commands/MergeItems/MergeItemsCommand.cs and GetItemMergePreviewQuery.cs. */
export type MergeItemsRequest = {
  primaryItemId: string;
  /** Includes the primary item. */
  itemIds: string[];
};

/** Mirrors ItemMergeKind (serialized camelCase). */
export type ItemMergeKind =
  | 'stockBatches'
  | 'stockTransactions'
  | 'goodsReceiptImportLines'
  | 'orderListLines'
  | 'classItemStockVisibilities'
  | 'itemLocationThresholds'
  | 'supplyListLines'
  | 'dailyItemConsumptions'
  | 'classBalances'
  | 'itemPurchaseStatistics';

export const ITEM_MERGE_KIND_LABELS: Record<ItemMergeKind, string> = {
  stockBatches: 'Loturi de stoc',
  stockTransactions: 'Tranzacții de stoc',
  goodsReceiptImportLines: 'Linii din importuri de recepții',
  orderListLines: 'Linii din liste de comandă',
  classItemStockVisibilities: 'Vizibilitate stoc pe clasă',
  itemLocationThresholds: 'Praguri pe locație',
  supplyListLines: 'Linii din liste de aprovizionare',
  dailyItemConsumptions: 'Consum zilnic',
  classBalances: 'Solduri de clasă',
  itemPurchaseStatistics: 'Statistici de achiziții'
};

/** Mirrors ItemMergeImpact. */
export type ItemMergeImpact = {
  kind: ItemMergeKind;
  /** Rows moved to the primary item. */
  relinked: number;
  /** Rows merged into another row (and deleted) because of a unique key collision. */
  combined: number;
};

/** Mirrors ItemMergePreviewDto. */
export type ItemMergePreviewDto = {
  impacts: ItemMergeImpact[];
};

export type ItemTableColumn =
  | 'sku'
  | 'name'
  | 'unit'
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
    label: 'Articol',
    value: 'name',
    fieldType: 'string'
  },
  unit: {
    label: 'Unitate',
    value: 'unit',
    fieldType: 'string'
  },
  isPerishable: {
    label: 'Perisabil',
    value: 'isPerishable',
    fieldType: 'boolean'
  },
  shelfLifeDays: {
    label: 'Valabilitate',
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

export type ItemDraft = Pick<ItemDto, 'name'>;

export type ItemAutocompleteValue = ItemDto | ItemDropdownOption | ItemDraft | null;

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
