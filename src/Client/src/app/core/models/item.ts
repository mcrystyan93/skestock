import { BasePaginationFilter, ColumnFilter } from './pagination';

/** Mirrors src/Application/Features/Items/Models/ItemDto.cs. */
export type ItemDto = {
  id: number;
  sku?: string | null;
  name: string;
  description?: string | null;
  unit: string;
  minThreshold: number;
  isPerishable: boolean;
  isActive: boolean;
  categoryId: number;
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
  categoryId: number;
};

/** Mirrors src/Application/Features/Items/Models/ItemRequests.cs. */
export type EditItemRequest = {
  sku?: string | null;
  name: string;
  description?: string | null;
  unit: string;
  minThreshold: number;
  isPerishable: boolean;
  categoryId: number;
};
