import { BasePaginationFilter, ColumnFilter, prioritizeSort, TableColumnDefinition } from './pagination';
import { PAGINATION_PAGE_SIZE } from './category';

/** Mirrors src/Application/Features/OrderLists/Models/OrderListLineDto.cs. */
export type OrderListLineDto = {
  id: string;
  itemId?: string | null;
  productName: string;
  quantity: number;
  unit?: string | null;
  notes?: string | null;
};

/** Mirrors src/Domain/Enums/OrderListStatus.cs. */
export type OrderListStatus = 'Draft' | 'Submitted' | 'Cancelled';

/** Mirrors src/Application/Features/OrderLists/Models/OrderListDto.cs. */
export type OrderListDto = {
  id: string;
  classId: string;
  className?: string | null;
  name?: string | null;
  note?: string | null;
  status: OrderListStatus;
  submittedAt?: string | null;
  lines: OrderListLineDto[];
  createdByName?: string | null;
  lastModifiedByName?: string | null;
  createdDate: string;
  lastModifiedDate: string;
};

/** Mirrors src/Application/Features/OrderLists/Models/OrderListListItemDto.cs. */
export type OrderListListItemDto = {
  id: string;
  classId: string;
  className?: string | null;
  name?: string | null;
  status: OrderListStatus;
  lineCount: number;
  submittedAt?: string | null;
  createdByName?: string | null;
  createdDate: string;
  lastModifiedDate: string;
};

/** Mirrors src/Application/Features/OrderLists/Models/OrderListRequests.cs. */
export type GetAllOrderListsRequest = BasePaginationFilter & {
  filters: ColumnFilter[];
};

/** Mirrors src/Application/Features/OrderLists/Models/OrderListRequests.cs. */
export type OrderListLineRequest = {
  itemId?: string | null;
  productName?: string | null;
  quantity: number;
  unit?: string | null;
  notes?: string | null;
};

/** Mirrors src/Application/Features/OrderLists/Models/OrderListRequests.cs. */
export type CreateOrderListRequest = {
  classId: string;
  name?: string | null;
  note?: string | null;
  lines: OrderListLineRequest[];
};

/** Mirrors src/Application/Features/OrderLists/Models/OrderListRequests.cs. */
export type UpdateOrderListRequest = {
  name?: string | null;
  note?: string | null;
  lines: OrderListLineRequest[];
};

export type OrderListTableColumn =
  | 'className'
  | 'name'
  | 'status'
  | 'lineCount'
  | 'submittedAt'
  | 'createdByName'
  | 'createdDate'
  | 'lastModifiedDate';

export const ORDER_LIST_TABLE_COLUMNS: TableColumnDefinition<OrderListTableColumn> = {
  className: {
    label: 'Clasa',
    value: 'className',
    fieldType: 'string'
  },
  name: {
    label: 'Nume',
    value: 'name',
    fieldType: 'string'
  },
  status: {
    label: 'Status',
    value: 'status',
    fieldType: 'select'
  },
  lineCount: {
    label: 'Nr. articole',
    value: 'lineCount',
    fieldType: 'number'
  },
  submittedAt: {
    label: 'Data trimiterii',
    value: 'submittedAt',
    fieldType: 'date'
  },
  createdByName: {
    label: 'Creat de',
    value: 'createdByName',
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
  }
};

export function buildOrderListFilter(
  currentFilter: GetAllOrderListsRequest,
  partialFilter: Partial<GetAllOrderListsRequest>
): GetAllOrderListsRequest {
  return {
    ...currentFilter,
    ...partialFilter,
    cursor: null,
    pageSize: partialFilter.pageSize ?? currentFilter.pageSize ?? PAGINATION_PAGE_SIZE,
    sort: prioritizeSort(currentFilter.sort ?? [], partialFilter.sort ?? [])
  };
}
