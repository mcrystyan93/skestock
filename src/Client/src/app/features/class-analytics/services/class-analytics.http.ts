import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import {
  ClassGoodsReceiptCostsDto,
  ClassStockByCategoryDto,
  GetClassLocationStockRequest,
} from '@ske/models';
import { StockHttp } from '@ske/shared/stock';
import { map } from 'rxjs';

@Service()
export class ClassAnalyticsHttp {
  private readonly _httpClient = inject(HttpClient);
  private readonly _stockHttp = inject(StockHttp);

  public getStockByCategory(classId: string, locationId: string | null) {
    const request: GetClassLocationStockRequest = {
      classId,
      filters: locationId
        ? [{
          field: 'locationId',
          value: locationId,
          operator: 'equals',
          fieldType: 'string'
        }]
        : [],
      searchTerm: null,
      includeHidden: false,
      lowStockOnly: false,
      expiredOnly: false
    };

    return this._stockHttp.getClassLocationStock(request).pipe(
      map((report): ClassStockByCategoryDto => {
        const positiveItems = report.items.filter((item) => item.quantity > 0);
        const itemIds = new Set(positiveItems.map((item) => item.itemId));
        const categoriesById = new Map<
          string,
          { categoryId: string; categoryName: string; quantity: number; itemIds: Set<string> }
        >();

        for (const item of positiveItems) {
          const category = categoriesById.get(item.categoryId) ?? {
            categoryId: item.categoryId,
            categoryName: item.categoryName,
            quantity: 0,
            itemIds: new Set<string>()
          };

          category.quantity += item.quantity;
          category.itemIds.add(item.itemId);
          categoriesById.set(item.categoryId, category);
        }

        const categories = [...categoriesById.values()]
          .map(({ itemIds: categoryItemIds, ...category }) => ({
            ...category,
            itemCount: categoryItemIds.size
          }))
          .sort((first, second) => first.categoryName.localeCompare(second.categoryName));

        return {
          categories,
          totalQuantity: categories.reduce((total, category) => total + category.quantity, 0),
          totalItemCount: itemIds.size,
          totalCategoryCount: categories.length
        };
      })
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
