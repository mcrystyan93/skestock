import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import {
  CreateItemRequest,
  EditItemRequest,
  GetAllItemsRequest,
  ItemDto,
  PaginatedResponse,
} from '@ske/models';

/**
 * HTTP client for src/Web/Endpoints/Items.cs, mapped under /api/Items.
 */
@Service()
export class ItemsHttp {
  private readonly _httpClient = inject(HttpClient);

  public getAll(request: GetAllItemsRequest) {
    return this._httpClient.post<PaginatedResponse<ItemDto>>('/api/Items/get-all', request);
  }

  public getById(id: number) {
    return this._httpClient.get<ItemDto>(`/api/Items/${id}`);
  }

  public create(request: CreateItemRequest) {
    return this._httpClient.post<ItemDto>('/api/Items', request);
  }

  public edit(id: number, request: EditItemRequest) {
    return this._httpClient.put<ItemDto>(`/api/Items/${id}`, request);
  }

  public enable(id: number) {
    return this._httpClient.patch<ItemDto>(`/api/Items/${id}/enable`, {});
  }

  public disable(id: number) {
    return this._httpClient.patch<ItemDto>(`/api/Items/${id}/disable`, {});
  }
}
