import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import {
  CategoryImportDto,
  CategoryImportBatchDto,
  CategoryImportBatchReviewDto,
  CategoryImportListItemDto,
  ConfirmCategoryImportBatchRequest,
  ConfirmCategoryImportBatchResponse,
  CategoryImportReviewDto,
  ConfirmCategoryImportRequest,
  ConfirmCategoryImportResponse,
  CreateCategoryImportBatchRequest,
  CreateCategoryImportRequest,
  GetAllCategoryImportsRequest,
  PaginatedResponse
} from '@ske/models';

@Service()
export class CategoryImportsHttp {
  private readonly _httpClient = inject(HttpClient);

  public create(request: CreateCategoryImportRequest) {
    return this._httpClient.post<CategoryImportDto>('/api/CategoryImports', request);
  }

  public createBatch(request: CreateCategoryImportBatchRequest) {
    return this._httpClient.post<CategoryImportBatchDto>('/api/CategoryImportBatches', request);
  }

  public getAll(request: GetAllCategoryImportsRequest | Partial<GetAllCategoryImportsRequest>) {
    return this._httpClient.post<PaginatedResponse<CategoryImportListItemDto>>('/api/CategoryImports/get-all', request);
  }

  public getById(id: string) {
    return this._httpClient.get<CategoryImportReviewDto>(`/api/CategoryImports/${id}`);
  }

  public confirm(id: string, request: ConfirmCategoryImportRequest) {
    return this._httpClient.post<ConfirmCategoryImportResponse>(`/api/CategoryImports/${id}/confirm`, request);
  }

  public getBatchById(id: string) {
    return this._httpClient.get<CategoryImportBatchReviewDto>(`/api/CategoryImportBatches/${id}`);
  }

  public confirmBatch(id: string, request: ConfirmCategoryImportBatchRequest) {
    return this._httpClient.post<ConfirmCategoryImportBatchResponse>(`/api/CategoryImportBatches/${id}/confirm`, request);
  }
}
