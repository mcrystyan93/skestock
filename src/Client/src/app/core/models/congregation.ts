import { BasePaginationFilter, ColumnFilter, prioritizeSort, TableColumnDefinition } from './pagination';
import { PAGINATION_PAGE_SIZE } from './category';

/** Mirrors src/Application/Features/Congregations/Models/CongregationDto.cs. */
export type CongregationDto = {
  id: string;
  name: string;
  email: string;
  coordinator?: string | null;
  coordinatorPhone?: string | null;
  dessertContactPerson?: string | null;
  dessertContactPhone?: string | null;
  canReceiveStudents: boolean;
  isSmallCongregation: boolean;
  message?: string | null;
  createdByName?: string | null;
  lastModifiedByName?: string | null;
  createdDate: string;
  lastModifiedDate: string;
};

/** Mirrors src/Application/Features/Congregations/Models/CongregationMutationDto.cs. */
export type CongregationMutationDto = Omit<
  CongregationDto,
  'createdByName' | 'lastModifiedByName' | 'createdDate' | 'lastModifiedDate'
>;

/** Mirrors src/Application/Features/Congregations/Models/CongregationRequests.cs. */
export type GetAllCongregationsRequest = BasePaginationFilter & {
  filters: ColumnFilter[];
};

/** Mirrors CongregationWriteRequest in src/Application/Features/Congregations/Models/CongregationRequests.cs. */
export type CongregationWriteRequest = {
  name: string;
  email: string;
  coordinator?: string | null;
  coordinatorPhone?: string | null;
  dessertContactPerson?: string | null;
  dessertContactPhone?: string | null;
  canReceiveStudents: boolean;
  isSmallCongregation: boolean;
  message?: string | null;
};

export type CreateCongregationRequest = CongregationWriteRequest;
export type UpdateCongregationRequest = CongregationWriteRequest;

export function buildCongregationListFilter(
  currentFilter: GetAllCongregationsRequest,
  partialFilter: Partial<GetAllCongregationsRequest>
): GetAllCongregationsRequest {
  return {
    ...currentFilter,
    ...partialFilter,
    cursor: null,
    pageSize: partialFilter.pageSize ?? currentFilter.pageSize ?? PAGINATION_PAGE_SIZE,
    sort: prioritizeSort(currentFilter.sort ?? [], partialFilter.sort ?? [])
  };
}

export type CongregationTableColumn =
  | 'name'
  | 'email'
  | 'coordinator'
  | 'canReceiveStudents'
  | 'isSmallCongregation'
  | 'createdDate'
  | 'lastModifiedDate'
  | 'actions';

export const CONGREGATION_TABLE_COLUMNS: TableColumnDefinition<CongregationTableColumn> = {
  name: {
    label: 'Nume',
    value: 'name',
    fieldType: 'string'
  },
  email: {
    label: 'Email',
    value: 'email',
    fieldType: 'string'
  },
  coordinator: {
    label: 'Coordonator',
    value: 'coordinator',
    fieldType: 'string'
  },
  canReceiveStudents: {
    label: 'Poate primi cursanți',
    value: 'canReceiveStudents',
    fieldType: 'boolean'
  },
  isSmallCongregation: {
    label: 'Congregație mică',
    value: 'isSmallCongregation',
    fieldType: 'boolean'
  },
  createdDate: {
    label: 'Data creare',
    value: 'createdDate',
    fieldType: 'date'
  },
  lastModifiedDate: {
    label: 'Ultima modificare',
    value: 'lastModifiedDate',
    fieldType: 'date'
  },
  actions: {
    label: 'Acțiuni',
    value: 'actions',
    fieldType: 'string'
  }
};

export type CongregationDropdownValue = CongregationDto | Partial<CongregationDto> | null;
