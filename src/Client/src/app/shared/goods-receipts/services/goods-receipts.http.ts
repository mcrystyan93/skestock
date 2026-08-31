import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import {
  CreateGoodsReceiptRequest,
  GetAllGoodsReceiptsRequest,
  GoodsReceiptDto,
  GoodsReceiptListItemDto,
  PaginatedResponse
} from '@ske/models';

/**
 * HTTP client for src/Web/Endpoints/GoodsReceipts.cs, mapped under /api/GoodsReceipts.
 */
@Service()
export class GoodsReceiptsHttp {
  private readonly _httpClient = inject(HttpClient);

  public getAll(request: GetAllGoodsReceiptsRequest | Partial<GetAllGoodsReceiptsRequest>) {
    return this._httpClient.post<PaginatedResponse<GoodsReceiptListItemDto>>('/api/GoodsReceipts/get-all', request);
  }

  public getById(id: number) {
    return this._httpClient.get<GoodsReceiptDto>(`/api/GoodsReceipts/${id}`);
  }

  public create(request: CreateGoodsReceiptRequest) {
    return this._httpClient.post<GoodsReceiptDto>('/api/GoodsReceipts', request);
  }
}
