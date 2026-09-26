import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import {
  ClassDailyConsumptionDto,
  ClassDailyConsumptionFilter,
  ClassItemStockEvolutionDto,
  ClassStockByCategoryChartDto,
  GetClassLocationStockRequest,
  PurchaseStatisticDto,
  StockReportDto,
  TopPurchasesDto,
  TopPurchasesFilter
} from '@ske/models';
import { StockHttp } from '@ske/shared/stock';
import { map } from 'rxjs';

@Service()
export class ClassStatisticsHttp {
  private readonly _httpClient = inject(HttpClient);
  private readonly _stockHttp = inject(StockHttp);

  public getStockByCategoryAllLocations(classId: string) {
    return this._httpClient.get<ClassStockByCategoryChartDto>(
      `/api/statistics/class/${classId}/stock-by-category`
    );
  }

  /** Drill-down of the all-locations chart: one bar per item at the location, coloured by category. */
  public getLocationStockByItem(classId: string, locationId: string) {
    return this._httpClient.get<ClassStockByCategoryChartDto>(
      `/api/statistics/class/${classId}/location/${locationId}/stock-by-item`
    );
  }

  public getClassStockItemIds(classId: string) {
    const request: GetClassLocationStockRequest = {
      classId,
      filters: [],
      searchTerm: null,
      includeHidden: false,
      lowStockOnly: false,
      expiredOnly: false
    };

    return this._stockHttp.getClassLocationStock(request).pipe(
      map((report: StockReportDto) => [...new Set(report.items.map((item) => item.itemId))])
    );
  }

  public getItemStockEvolution(classId: string, itemId: string) {
    return this._httpClient.get<ClassItemStockEvolutionDto>(
      `/api/statistics/class/${classId}/item/${itemId}/stock-evolution`
    );
  }

  public getClassDailyConsumption(classId: string, filter: ClassDailyConsumptionFilter) {
    let params = new HttpParams();
    if (filter.itemId) params = params.set('itemId', filter.itemId);
    if (filter.locationId) params = params.set('locationId', filter.locationId);
    if (filter.categoryId) params = params.set('categoryId', filter.categoryId);

    return this._httpClient.get<ClassDailyConsumptionDto>(
      `/api/statistics/class/${classId}/daily-consumption`,
      { params }
    );
  }

  public getTopPurchases(filter: TopPurchasesFilter) {
    let params = new HttpParams().set('scope', filter.scope);
    if (filter.scope === 'Class' && filter.classId) params = params.set('classId', filter.classId);
    if (filter.categoryId) params = params.set('categoryId', filter.categoryId);
    if (filter.top) params = params.set('top', filter.top);

    return this._httpClient.get<TopPurchasesDto>('/api/statistics/purchases/top', { params });
  }

  public getItemsPurchaseHistory(itemIds: string[]) {
    return this._httpClient.post<PurchaseStatisticDto[]>(
      '/api/statistics/purchases/items-history',
      { itemIds }
    );
  }
}
