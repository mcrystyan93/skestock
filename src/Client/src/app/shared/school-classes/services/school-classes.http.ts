import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import {
  CreateSchoolClassRequest,
  GetAllSchoolClassesRequest,
  PaginatedResponse,
  SchoolClassDto,
  SchoolClassSummary,
  UpdateSchoolClassRequest
} from '@ske/models';

/**
 * HTTP client for src/Web/Endpoints/SchoolClasses.cs, mapped under /api/SchoolClasses.
 */
@Service()
export class SchoolClassesHttp {
  private readonly _httpClient = inject(HttpClient);

  public getAll(request: GetAllSchoolClassesRequest) {
    return this._httpClient.post<PaginatedResponse<SchoolClassDto>>('/api/SchoolClasses/get-all', request);
  }

  public getById(id: number) {
    return this._httpClient.get<SchoolClassDto>(`/api/SchoolClasses/${id}`);
  }

  public getSummary(id: number) {
    return this._httpClient.get<SchoolClassSummary>(`/api/SchoolClasses/${id}/summary`);
  }

  public create(request: CreateSchoolClassRequest) {
    return this._httpClient.post<SchoolClassDto>('/api/SchoolClasses', request);
  }

  public update(id: number, request: UpdateSchoolClassRequest) {
    return this._httpClient.put<SchoolClassDto>(`/api/SchoolClasses/${id}`, request);
  }
}
