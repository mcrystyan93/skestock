import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { AdjustStockRequest, GetClassLocationStockRequest, StockItemDto } from '@ske/models';

/**
 * HTTP client for src/Web/Endpoints/Stock.cs, mapped under /api/Stock.
 */
@Service()
export class StockHttp {
  private readonly _httpClient = inject(HttpClient);

  public getClassLocationStock(request: GetClassLocationStockRequest) {
    const { classId, locationId, searchTerm } = request;

    let params = new HttpParams();
    if (locationId != null) {
      params = params.set('locationId', locationId);
    }
    if (searchTerm != null && searchTerm !== '') {
      params = params.set('searchTerm', searchTerm);
    }

    return this._httpClient.get<StockItemDto[]>(`/api/Stock/class/${classId}`, { params });
  }

  public adjustStock(request: AdjustStockRequest) {
    return this._httpClient.post<StockItemDto>('/api/Stock/adjust', request);
  }
}
