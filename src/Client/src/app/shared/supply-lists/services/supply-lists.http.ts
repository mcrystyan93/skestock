import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { EMPTY, expand, reduce } from 'rxjs';
import {
  CreateSupplyListRequest,
  GetAllSupplyListsRequest,
  PAGINATION_PAGE_SIZE,
  PaginatedResponse,
  SupplyListDto,
  SupplyListListItemDto,
  UpdateSupplyListRequest
} from '@ske/models';

/**
 * HTTP client for src/Web/Endpoints/SupplyLists.cs, mapped under /api/SupplyLists.
 */
@Service()
export class SupplyListsHttp {
  private readonly _httpClient = inject(HttpClient);

  public getAll(request: GetAllSupplyListsRequest | Partial<GetAllSupplyListsRequest>) {
    return this._httpClient.post<PaginatedResponse<SupplyListListItemDto>>('/api/SupplyLists/get-all', request);
  }

  // The API caps the page size, so the whole active set is read by following the cursor.
  public getAllActive() {
    const page = (cursor: string | null) => this.getAll({
      pageSize: PAGINATION_PAGE_SIZE,
      cursor,
      filters: [{ field: 'isActive', operator: 'equals', value: true, fieldType: 'boolean' }],
      sort: [{ key: 'name', value: 'ascend' }]
    });

    return page(null).pipe(
      expand((response) => response.hasNextPage && response.nextCursor ? page(response.nextCursor) : EMPTY),
      reduce((all, response) => [...all, ...response.data], [] as SupplyListListItemDto[])
    );
  }

  public getById(id: string) {
    return this._httpClient.get<SupplyListDto>(`/api/SupplyLists/${id}`);
  }

  public create(request: CreateSupplyListRequest) {
    return this._httpClient.post<SupplyListDto>('/api/SupplyLists', request);
  }

  public update(id: string, request: UpdateSupplyListRequest) {
    return this._httpClient.put<SupplyListDto>(`/api/SupplyLists/${id}`, request);
  }

  public disable(id: string) {
    return this._httpClient.post<SupplyListDto>(`/api/SupplyLists/${id}/disable`, {});
  }

  public enable(id: string) {
    return this._httpClient.post<SupplyListDto>(`/api/SupplyLists/${id}/enable`, {});
  }
}
