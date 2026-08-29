import { BasePaginationFilter, ColumnFilter } from './pagination';

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
  id: number;
  name: string;
  startDate: string;
  endDate: string;
  status: ClassStatus;
  createdByName?: string | null;
  lastModifiedByName?: string | null;
  createdDate: string;
  lastModifiedDate: string;
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
