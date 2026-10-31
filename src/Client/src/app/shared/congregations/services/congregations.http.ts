import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import {
  CongregationDto,
  CongregationMutationDto,
  CreateCongregationRequest,
  GetAllCongregationsRequest,
  PaginatedResponse,
  UpdateCongregationRequest
} from '@ske/models';

/**
 * HTTP client for src/Web/Endpoints/Congregations.cs, mapped under /api/Congregations.
 */
@Service()
export class CongregationsHttp {
  private readonly _httpClient = inject(HttpClient);

  public getAll(request: GetAllCongregationsRequest) {
    return this._httpClient.post<PaginatedResponse<CongregationDto>>('/api/Congregations/get-all', request);
  }

  public getById(id: string) {
    return this._httpClient.get<CongregationDto>(`/api/Congregations/${id}`);
  }

  public create(request: CreateCongregationRequest) {
    return this._httpClient.post<CongregationMutationDto>('/api/Congregations', request);
  }

  public update(id: string, request: UpdateCongregationRequest) {
    return this._httpClient.put<CongregationMutationDto>(`/api/Congregations/${id}`, request);
  }

  public delete(id: string) {
    return this._httpClient.delete<void>(`/api/Congregations/${id}`);
  }
}
