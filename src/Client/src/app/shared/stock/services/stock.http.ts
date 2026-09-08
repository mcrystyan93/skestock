import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { AdjustStockRequest, GetClassLocationStockRequest, StockItemDto } from '@ske/models';

/**
 * HTTP client for src/Web/Endpoints/Stock.cs, mapped under /api/Stock.
 */
@Service()
export class StockHttp {
  private readonly _httpClient = inject(HttpClient);

  public getClassLocationStock(request: GetClassLocationStockRequest) {
    const { classId, ...body } = request;

    return this._httpClient.post<StockItemDto[]>(`/api/Stock/class/${classId}`, body);
  }

  public adjustStock(request: AdjustStockRequest) {
    return this._httpClient.post<StockItemDto>('/api/Stock/adjust', request);
  }
}
