import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import {
  CreateSupplyListRequest,
  GetAllSupplyListsRequest,
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
