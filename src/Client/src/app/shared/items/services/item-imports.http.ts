import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import {
  ConfirmItemImportRequest,
  CreateItemImportRequest,
  GetAllItemImportsRequest,
  ItemImportConfirmationResultDto,
  ItemImportDto,
  ItemImportListItemDto,
  ItemImportReviewDto,
  PaginatedResponse
} from '@ske/models';

@Service()
export class ItemImportsHttp {
  private readonly _httpClient = inject(HttpClient);

  public create(request: CreateItemImportRequest) {
    return this._httpClient.post<ItemImportDto>('/api/ItemImports', request);
  }

  public getAll(request: GetAllItemImportsRequest | Partial<GetAllItemImportsRequest>) {
    return this._httpClient.post<PaginatedResponse<ItemImportListItemDto>>('/api/ItemImports/get-all', request);
  }

  public getById(id: string) {
    return this._httpClient.get<ItemImportReviewDto>(`/api/ItemImports/${id}`);
  }

  public confirm(id: string, request: ConfirmItemImportRequest) {
    return this._httpClient.post<ItemImportConfirmationResultDto>(`/api/ItemImports/${id}/confirm`, request);
  }
}
