import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import {
  CreateLocationRequest,
  DefaultLocationDto,
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

  public getById(id: string) {
    return this._httpClient.get<LocationDto>(`/api/Locations/${id}`);
  }

  public getDefault() {
    return this._httpClient.get<DefaultLocationDto>('/api/Locations/default');
  }

  public create(request: CreateLocationRequest) {
    return this._httpClient.post<LocationDto>('/api/Locations', request);
  }

  public update(id: string, request: UpdateLocationRequest) {
    return this._httpClient.put<LocationDto>(`/api/Locations/${id}`, request);
  }
}
