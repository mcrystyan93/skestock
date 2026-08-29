import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import {
  CategoryDto,
  CreateCategoryRequest,
  GetAllCategoriesRequest,
  PaginatedResponse,
  UpdateCategoryRequest
} from '@ske/models';

/**
 * HTTP client for src/Web/Endpoints/Categories.cs, mapped under /api/Categories.
 */
@Service()
export class CategoriesHttp {
  private readonly _httpClient = inject(HttpClient);

  public getAll(request: GetAllCategoriesRequest) {
    return this._httpClient.post<PaginatedResponse<CategoryDto>>('/api/Categories/get-all', request);
  }

  public getById(id: number) {
    return this._httpClient.get<CategoryDto>(`/api/Categories/${id}`);
  }

  public create(request: CreateCategoryRequest) {
    return this._httpClient.post<CategoryDto>('/api/Categories', request);
  }

  public update(id: number, request: UpdateCategoryRequest) {
    return this._httpClient.put<CategoryDto>(`/api/Categories/${id}`, request);
  }
}
