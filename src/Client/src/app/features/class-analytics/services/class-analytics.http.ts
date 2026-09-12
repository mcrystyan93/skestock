import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import {
  ClassGoodsReceiptCostsDto,
  ClassStockByCategoryDto,
  GetAllSchoolClassesRequest,
  LocationDto,
  PaginatedResponse,
} from '@ske/models';
import { SchoolClassesHttp } from '@ske/shared/school-classes';

const LOCATION_PAGE_SIZE = 50;

@Service()
export class ClassAnalyticsHttp {
  private readonly _httpClient = inject(HttpClient);


  public getStockByCategory(classId: string, locationId: string | null) {
    let params = new HttpParams();
    if (locationId) {
      params = params.set('locationId', locationId);
    }

    return this._httpClient.get<ClassStockByCategoryDto>(
      `/api/statistics/class/${classId}/stock-by-category`,
      { params }
    );
  }

  public getGoodsReceiptCosts(classId: string, startDate: string | null, endDate: string | null) {
    let params = new HttpParams();
    if (startDate) {
      params = params.set('startDate', startDate);
    }
    if (endDate) {
      params = params.set('endDate', endDate);
    }

    return this._httpClient.get<ClassGoodsReceiptCostsDto>(
      `/api/statistics/class/${classId}/goods-receipt-costs`,
      { params }
    );
  }
}
