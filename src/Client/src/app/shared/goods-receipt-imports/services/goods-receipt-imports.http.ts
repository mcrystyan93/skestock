import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { GetAllGoodsReceiptImportsRequest, GoodsReceiptImportListItemDto, PaginatedResponse } from '@ske/models';

/**
 * HTTP client for src/Web/Endpoints/GoodsReceipts.cs, mapped under /api/GoodsReceipts/imports.
 */
@Service()
export class GoodsReceiptImportsHttp {
  private readonly _httpClient = inject(HttpClient);

  public getAll(request: GetAllGoodsReceiptImportsRequest | Partial<GetAllGoodsReceiptImportsRequest>) {
    return this._httpClient.post<PaginatedResponse<GoodsReceiptImportListItemDto>>('/api/GoodsReceipts/imports/get-all', request);
  }
}
