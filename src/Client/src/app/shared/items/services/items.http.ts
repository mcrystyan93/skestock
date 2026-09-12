import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import {
  CreateItemRequest,
  EditItemRequest,
  GetAllItemsRequest,
  ItemDto,
  PaginatedResponse,
} from '@ske/models';
import { catchError, of, shareReplay, tap, throwError, type Observable } from 'rxjs';

/**
 * HTTP client for src/Web/Endpoints/Items.cs, mapped under /api/Items.
 */
@Service()
export class ItemsHttp {
  private readonly _httpClient = inject(HttpClient);
  private readonly _itemByIdCache = new Map<string, Observable<ItemDto>>();

  public getAll(request: GetAllItemsRequest) {
    return this._httpClient.post<PaginatedResponse<ItemDto>>('/api/Items/get-all', request);
  }

  public getById(id: string) {
    return this._httpClient.get<ItemDto>(`/api/Items/${id}`);
  }

  public getByIdCached(id: string): Observable<ItemDto> {
    const cachedItem = this._itemByIdCache.get(id);

    if (cachedItem)
      return cachedItem;

    const request = this.getById(id).pipe(
      tap((item) => this.cacheItem(item)),
      catchError((error: unknown) => {
        this._itemByIdCache.delete(id);
        return throwError(() => error);
      }),
      shareReplay({ bufferSize: 1, refCount: false })
    );

    this._itemByIdCache.set(id, request);
    return request;
  }

  public create(request: CreateItemRequest) {
    return this._httpClient.post<ItemDto>('/api/Items', request).pipe(
      tap((item) => this.cacheItem(item))
    );
  }

  public edit(id: string, request: EditItemRequest) {
    return this._httpClient.put<ItemDto>(`/api/Items/${id}`, request).pipe(
      tap((item) => this.cacheItem(item))
    );
  }

  public enable(id: string) {
    return this._httpClient.patch<ItemDto>(`/api/Items/${id}/enable`, {}).pipe(
      tap((item) => this.cacheItem(item))
    );
  }

  public disable(id: string) {
    return this._httpClient.patch<ItemDto>(`/api/Items/${id}/disable`, {}).pipe(
      tap((item) => this.cacheItem(item))
    );
  }

  private cacheItem(item: ItemDto) {
    this._itemByIdCache.set(item.id, of(item));
  }
}
