import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import {
  CreateLocationRequest,
  GetAllLocationsRequest,
  LocationDto,
  PaginatedResponse,
  UpdateLocationRequest
} from '@ske/models';

/**
 * HTTP client for src/Web/Endpoints/Locations.cs, mapped under /api/Locations.
 */
@Service()
export class LocationsHttp {
  private readonly _httpClient = inject(HttpClient);

  public getAll(request: GetAllLocationsRequest) {
    return this._httpClient.post<PaginatedResponse<LocationDto>>('/api/Locations/get-all', request);
  }

  public getById(id: number) {
    return this._httpClient.get<LocationDto>(`/api/Locations/${id}`);
  }

  public create(request: CreateLocationRequest) {
    return this._httpClient.post<LocationDto>('/api/Locations', request);
  }

  public update(id: number, request: UpdateLocationRequest) {
    return this._httpClient.put<LocationDto>(`/api/Locations/${id}`, request);
  }
}
