import {Component, effect, inject, untracked} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {toSignal} from '@angular/core/rxjs-interop';
import {ClassAnalyticsStore} from './services/class-analytics.store';
import {ClassAnalyticsFilters} from './ui/class-analytics-filters';
import {ClassAnalyticsKpis} from './ui/class-analytics-kpis';
import {CategoryStockChart} from './ui/category-stock-chart';
import {ReceiptCostChart} from './ui/receipt-cost-chart';
import {GoodsReceiptCostPointDto, CategoryStockSummaryDto} from '@ske/models';
import {ErrorAlert} from '@ske/shared/errors';
import {Header} from './ui/header/header';
import { QueryParamState } from '@ske/routes';

const DATE_PATTERN = /^\d{4}-\d{2}-\d{2}$/;

function readDate(value: string | null): string | null {
  return value && DATE_PATTERN.test(value) ? value : null;
}

@Component({
  selector: 'ske-class-analytics-page',
  imports: [
    ClassAnalyticsFilters,
    ClassAnalyticsKpis,
    CategoryStockChart,
    ReceiptCostChart,
    ErrorAlert,
    Header
  ],
  templateUrl: './class-analytics.page.html',
  providers: [ClassAnalyticsStore, QueryParamState],
  host: {
    class: 'flex min-h-0 grow flex-col overflow-auto gap-4'
  }
})
export class ClassAnalyticsPage {
  public readonly store = inject(ClassAnalyticsStore);

  private readonly _route = inject(ActivatedRoute);
  private readonly _router = inject(Router);
  // private readonly _queryParams = toSignal(this._route.queryParamMap, {
  //   initialValue: this._route.snapshot.queryParamMap
  // });
  // private _initialized = false;

  // private readonly _initializeEffect = effect(() => {
  //   const params = this._queryParams();
  //   if (this._initialized) {
  //     return;
  //   }
  //
  //   this._initialized = true;
  //   untracked(() => this.store.initialize({
  //     classId: params.get('classId'),
  //     locationId: params.get('locationId'),
  //     startDate: readDate(params.get('startDate')),
  //     endDate: readDate(params.get('endDate'))
  //   }));
  // });

  // public dateRangeChanged(range: { startDate: string | null; endDate: string | null }): void {
  //   this.store.setDateRange(range.startDate, range.endDate);
  //   this.updateQuery({
  //     startDate: range.startDate,
  //     endDate: range.endDate
  //   });
  // }
  //
  // public categorySelected(category: CategoryStockSummaryDto): void {
  //   const classId = this.store.selectedClassId();
  //   if (!classId) {
  //     return;
  //   }
  //
  //   void this._router.navigate(['/school-classes', classId], {
  //     queryParams: {
  //       tab: 'stock',
  //       categoryId: category.categoryId,
  //       locationId: this.store.selectedLocationId()
  //     }
  //   });
  // }
  //
  // public receiptSelected(point: GoodsReceiptCostPointDto): void {
  //   const classId = this.store.selectedClassId();
  //   if (!classId) {
  //     return;
  //   }
  //
  //   void this._router.navigate(['/school-classes', classId], {
  //     queryParams: {
  //       tab: 'receipts',
  //       receiptId: point.id
  //     }
  //   });
  // }
  //
  // private updateQuery(queryParams: Record<string, string | null>): void {
  //   void this._router.navigate([], {
  //     relativeTo: this._route,
  //     queryParams,
  //     queryParamsHandling: 'merge',
  //     replaceUrl: true
  //   });
  // }
}
