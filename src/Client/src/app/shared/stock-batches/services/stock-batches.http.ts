import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { GetAllStockBatchesRequest, PaginatedResponse, StockBatchListItemDto } from '@ske/models';

/**
 * HTTP client for src/Web/Endpoints/StockBatches.cs, mapped under /api/StockBatches.
 */
@Service()
export class StockBatchesHttp {
  private readonly _httpClient = inject(HttpClient);

  public getAll(request: GetAllStockBatchesRequest | Partial<GetAllStockBatchesRequest>) {
    return this._httpClient.post<PaginatedResponse<StockBatchListItemDto>>('/api/StockBatches/get-all', request);
  }
}
