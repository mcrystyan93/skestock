import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { EMPTY, expand, reduce } from 'rxjs';
import {
  CreateLocationRequest,
  DefaultLocationDto,
  GetAllLocationsRequest,
  LocationDto,
  PaginatedResponse,
  UpdateLocationRequest,
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

  /** Fetches every page; used where all locations are needed at once (e.g. the per-location threshold form). */
  public getAllPages(pageSize = 100) {
    const fetchPage = (cursor: string | null) => this.getAll({ filters: [], pageSize, cursor });

    return fetchPage(null).pipe(
      expand((page) => (page.hasNextPage && page.nextCursor ? fetchPage(page.nextCursor) : EMPTY)),
      reduce((all, page) => [...all, ...page.data], [] as LocationDto[]),
    );
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
