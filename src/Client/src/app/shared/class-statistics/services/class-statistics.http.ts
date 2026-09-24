import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import {
  ClassItemStockEvolutionDto,
  ClassStockByCategoryChartDto,
  GetClassLocationStockRequest,
  StockReportDto
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

  public getStockByCategoryForLocation(classId: string, locationId: string) {
    return this._httpClient.get<ClassStockByCategoryChartDto>(
      `/api/statistics/class/${classId}/location/${locationId}/stock-by-category`
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
}
