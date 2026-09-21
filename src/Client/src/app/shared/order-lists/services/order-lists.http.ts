import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import {
  CreateOrderListRequest,
  GetAllOrderListsRequest,
  OrderListDto,
  OrderListListItemDto,
  PaginatedResponse,
  UpdateOrderListRequest
} from '@ske/models';

/**
 * HTTP client for src/Web/Endpoints/OrderLists.cs, mapped under /api/OrderLists.
 */
@Service()
export class OrderListsHttp {
  private readonly _httpClient = inject(HttpClient);

  public getAll(request: GetAllOrderListsRequest | Partial<GetAllOrderListsRequest>) {
    return this._httpClient.post<PaginatedResponse<OrderListListItemDto>>('/api/OrderLists/get-all', request);
  }

  public getById(id: string) {
    return this._httpClient.get<OrderListDto>(`/api/OrderLists/${id}`);
  }

  public create(request: CreateOrderListRequest) {
    return this._httpClient.post<OrderListDto>('/api/OrderLists', request);
  }

  public update(id: string, request: UpdateOrderListRequest) {
    return this._httpClient.put<OrderListDto>(`/api/OrderLists/${id}`, request);
  }

  public submit(id: string) {
    return this._httpClient.post<OrderListDto>(`/api/OrderLists/${id}/submit`, {});
  }

  public cancel(id: string) {
    return this._httpClient.post<OrderListDto>(`/api/OrderLists/${id}/cancel`, {});
  }

  public reopen(id: string) {
    return this._httpClient.post<OrderListDto>(`/api/OrderLists/${id}/reopen`, {});
  }

  public delete(id: string) {
    return this._httpClient.delete<void>(`/api/OrderLists/${id}`);
  }
}
