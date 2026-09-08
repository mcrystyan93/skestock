import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import {
  CategoryImportDto,
  CategoryImportReviewDto,
  ConfirmCategoryImportRequest,
  ConfirmCategoryImportResponse,
  CreateCategoryImportRequest
} from '@ske/models';

@Service()
export class CategoryImportsHttp {
  private readonly _httpClient = inject(HttpClient);

  public create(request: CreateCategoryImportRequest) {
    return this._httpClient.post<CategoryImportDto>('/api/CategoryImports', request);
  }

  public getById(id: string) {
    return this._httpClient.get<CategoryImportReviewDto>(`/api/CategoryImports/${id}`);
  }

  public confirm(id: string, request: ConfirmCategoryImportRequest) {
    return this._httpClient.post<ConfirmCategoryImportResponse>(`/api/CategoryImports/${id}/confirm`, request);
  }
}
