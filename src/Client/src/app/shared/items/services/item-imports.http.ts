import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import {
  ConfirmItemImportBatchRequest,
  ConfirmItemImportBatchResponse,
  CreateItemImportBatchRequest,
  ItemImportBatchDto,
  ItemImportBatchReviewDto,
  GetAllItemImportBatchesRequest,
  ItemImportBatchListItemDto,
  PaginatedResponse
} from '@ske/models';

@Service()
export class ItemImportsHttp {
  private readonly _httpClient = inject(HttpClient);

  public createBatch(request: CreateItemImportBatchRequest) {
    return this._httpClient.post<ItemImportBatchDto>('/api/ItemImportBatches', request);
  }

  public getAll(request: GetAllItemImportBatchesRequest | Partial<GetAllItemImportBatchesRequest>) {
    return this._httpClient.post<PaginatedResponse<ItemImportBatchListItemDto>>(
      '/api/ItemImportBatches/get-all',
      request
    );
  }

  public getBatchById(id: string) {
    return this._httpClient.get<ItemImportBatchReviewDto>(`/api/ItemImportBatches/${id}`);
  }

  public confirmBatch(id: string, request: ConfirmItemImportBatchRequest) {
    return this._httpClient.post<ConfirmItemImportBatchResponse>(`/api/ItemImportBatches/${id}/confirm`, request);
  }
}
