import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import {
  CategoryImportDto,
  CategoryImportListItemDto,
  CategoryImportReviewDto,
  ConfirmCategoryImportRequest,
  ConfirmCategoryImportResponse,
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

  public getAll(request: GetAllCategoryImportsRequest | Partial<GetAllCategoryImportsRequest>) {
    return this._httpClient.post<PaginatedResponse<CategoryImportListItemDto>>('/api/CategoryImports/get-all', request);
  }

  public getById(id: string) {
    return this._httpClient.get<CategoryImportReviewDto>(`/api/CategoryImports/${id}`);
  }

  public confirm(id: string, request: ConfirmCategoryImportRequest) {
    return this._httpClient.post<ConfirmCategoryImportResponse>(`/api/CategoryImports/${id}/confirm`, request);
  }
}
