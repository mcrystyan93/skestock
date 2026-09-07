import { BasePaginationFilter, ColumnFilter, prioritizeSort } from './pagination';
import { PAGINATION_PAGE_SIZE } from './category';

/** Mirrors src/Application/Features/Locations/Models/LocationDto.cs. */
export type LocationDto = {
  id: string;
  name: string;
  type: string;
  isDefault: boolean;
  parentLocationId?: string | null;
  parentLocationName?: string | null;
  createdByName?: string | null;
  lastModifiedByName?: string | null;
  createdDate: string;
  lastModifiedDate: string;
};

/** Payload of GET /api/Locations/default — null when no default is configured. */
export type DefaultLocationDto = LocationDto | null;

/** Mirrors src/Application/Features/Locations/Models/LocationRequests.cs. */
export type GetAllLocationsRequest = BasePaginationFilter & {
  filters: ColumnFilter[];
};

/** Mirrors src/Application/Features/Locations/Models/LocationRequests.cs. */
export type CreateLocationRequest = {
  name: string;
  type: string;
  parentLocationId?: string | null;
};

/** Mirrors src/Application/Features/Locations/Models/LocationRequests.cs. */
export type UpdateLocationRequest = {
  name: string;
  type: string;
  parentLocationId?: string | null;
};

export type LocationDropdownValue = LocationDto | Partial<LocationDto> | null;

export function buildLocationListFilter(
  currentFilter: GetAllLocationsRequest,
  partialFilter: Partial<GetAllLocationsRequest>
): GetAllLocationsRequest {
  return {
    ...currentFilter,
    ...partialFilter,
    cursor: null,
    pageSize: partialFilter.pageSize ?? currentFilter.pageSize ?? PAGINATION_PAGE_SIZE,
    sort: prioritizeSort(currentFilter.sort ?? [], partialFilter.sort ?? [])
  };
}
