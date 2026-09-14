import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import {
  CategoryImportBatchDto,
  CategoryImportBatchReviewDto,
  CategoryImportBatchListItemDto,
  ConfirmCategoryImportBatchRequest,
  ConfirmCategoryImportBatchResponse,
  CreateCategoryImportBatchRequest,
  GetAllCategoryImportBatchesRequest,
  PaginatedResponse
} from '@ske/models';

@Service()
export class CategoryImportsHttp {
  private readonly _httpClient = inject(HttpClient);

  public createBatch(request: CreateCategoryImportBatchRequest) {
    return this._httpClient.post<CategoryImportBatchDto>('/api/CategoryImportBatches', request);
  }

  public getAll(request: GetAllCategoryImportBatchesRequest | Partial<GetAllCategoryImportBatchesRequest>) {
    return this._httpClient.post<PaginatedResponse<CategoryImportBatchListItemDto>>(
      '/api/CategoryImportBatches/get-all',
      request
    );
  }

  public getBatchById(id: string) {
    return this._httpClient.get<CategoryImportBatchReviewDto>(`/api/CategoryImportBatches/${id}`);
  }

  public confirmBatch(id: string, request: ConfirmCategoryImportBatchRequest) {
    return this._httpClient.post<ConfirmCategoryImportBatchResponse>(`/api/CategoryImportBatches/${id}/confirm`, request);
  }
}
