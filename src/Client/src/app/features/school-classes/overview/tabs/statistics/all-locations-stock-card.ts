import {Component, inject} from '@angular/core';
import {NzCardComponent} from 'ng-zorro-antd/card';
import {ErrorAlert} from '@ske/shared/errors';
import {ClassStatisticsStore} from '../../../services/class-statistics.store';
import {ClassStockByCategoryChart} from './class-stock-by-category-chart';
import {Loader, LoaderDirective} from '@ske/shared/loader';

@Component({
  imports: [NzCardComponent, ErrorAlert, ClassStockByCategoryChart, LoaderDirective],
  selector: 'ske-all-locations-stock-card',
  template: `
    <nz-card class="min-h-110 flex-body has-chart"
             nzTitle="Stoc pe categorii în toate locațiile">
      <ng-container *skeLoader="store.allLocationsLoading()">
        @if (store.allLocationsProblemDetail() || store.allLocationsValidationErrors()) {
          <ske-error-display [problemDetail]="store.allLocationsProblemDetail()"
                             [validationErrors]="store.allLocationsValidationErrors()"/>
        } @else {
          <ske-class-stock-by-category-chart [data]="store.allLocationsChart()"
                                             emptyMessage="Nu există stoc înregistrat pentru această clasă."
                                             ariaLabel="Stoc pe categorii în toate locațiile"/>
        }
      </ng-container>
    </nz-card>
  `
})
export class AllLocationsStockCard {
  public readonly store = inject(ClassStatisticsStore);
}
