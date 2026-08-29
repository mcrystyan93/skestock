import { BasePaginationFilter, ColumnFilter } from './pagination';

/** Mirrors src/Application/Features/Locations/Models/LocationDto.cs. */
export type LocationDto = {
  id: number;
  name: string;
  type: string;
  parentLocationId?: number | null;
  parentLocationName?: string | null;
  createdByName?: string | null;
  lastModifiedByName?: string | null;
  createdDate: string;
  lastModifiedDate: string;
};

/** Mirrors src/Application/Features/Locations/Models/LocationRequests.cs. */
export type GetAllLocationsRequest = BasePaginationFilter & {
  filters: ColumnFilter[];
};

/** Mirrors src/Application/Features/Locations/Models/LocationRequests.cs. */
export type CreateLocationRequest = {
  name: string;
  type: string;
  parentLocationId?: number | null;
};

/** Mirrors src/Application/Features/Locations/Models/LocationRequests.cs. */
export type UpdateLocationRequest = {
  name: string;
  type: string;
  parentLocationId?: number | null;
};
