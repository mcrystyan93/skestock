import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import {
  AdjustStockRequest,
  GetClassLocationStockRequest,
  LowStockItemDto,
  MoveStockRequest,
  RemoveExpiredStockRequest,
  SetClassItemStockVisibilityRequest,
  StockItemDto,
  StockReportDto
} from '@ske/models';

/**
 * HTTP client for src/Web/Endpoints/Stock.cs, mapped under /api/Stock.
 */
@Service()
export class StockHttp {
  private readonly _httpClient = inject(HttpClient);

  public getClassLocationStock(request: GetClassLocationStockRequest) {
    const { classId, ...body } = request;

    return this._httpClient.post<StockReportDto>(`/api/Stock/class/${classId}`, body);
  }

  public getLowStockItems(classId: string) {
    return this._httpClient.get<LowStockItemDto[]>(`/api/Stock/class/${classId}/low-stock`);
  }

  public adjustStock(request: AdjustStockRequest) {
    return this._httpClient.post<StockItemDto>('/api/Stock/adjust', request);
  }

  public removeExpiredStock(request: RemoveExpiredStockRequest) {
    return this._httpClient.post<void>('/api/Stock/remove-expired', request);
  }

  public moveStock(request: MoveStockRequest) {
    return this._httpClient.post<void>('/api/Stock/move', request);
  }

  public setClassItemStockVisibility(
    classId: string,
    itemId: string,
    locationId: string,
    request: SetClassItemStockVisibilityRequest
  ) {
    return this._httpClient.patch<void>(
      `/api/Stock/class/${classId}/item/${itemId}/location/${locationId}/visibility`,
      request
    );
  }
}
