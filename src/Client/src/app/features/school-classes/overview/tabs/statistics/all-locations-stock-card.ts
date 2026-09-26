import {Component, computed, inject} from '@angular/core';
import {NzButtonComponent} from 'ng-zorro-antd/button';
import {NzCardComponent} from 'ng-zorro-antd/card';
import {NzTypographyComponent} from 'ng-zorro-antd/typography';
import {ErrorAlert} from '@ske/shared/errors';
import {LoaderDirective} from '@ske/shared/loader';
import {ClassStatisticsStore} from '../../../services/class-statistics.store';
import {ClassStockByCategoryBar, ClassStockByCategoryChart} from './class-stock-by-category-chart';

/**
 * Class stock card with a one-level drill-down:
 * - overview: one bar per location, stacked by category; clicking a bar opens that location;
 * - location: one bar per item at the location, coloured by category, with a back button.
 */
@Component({
  imports: [
    NzButtonComponent,
    NzCardComponent,
    NzTypographyComponent,
    ErrorAlert,
    ClassStockByCategoryChart,
    LoaderDirective
  ],
  selector: 'ske-all-locations-stock-card',
  template: `
    <nz-card class="min-h-110 flex-body has-chart"
             [nzTitle]="title()"
             [nzExtra]="extra">
      @if (store.drilledLocation(); as location) {
        <ng-container *skeLoader="store.locationLoading()">
          @if (store.locationProblemDetail() || store.locationValidationErrors()) {
            <ske-error-display [problemDetail]="store.locationProblemDetail()"
                               [validationErrors]="store.locationValidationErrors()"/>
          } @else {
            <ske-class-stock-by-category-chart [data]="store.locationChart()"
                                               emptyMessage="Nu există stoc pentru această clasă în locația selectată."
                                               [ariaLabel]="title()"/>
          }
        </ng-container>
      } @else {
        <ng-container *skeLoader="store.allLocationsLoading()">
          @if (store.allLocationsProblemDetail() || store.allLocationsValidationErrors()) {
            <ske-error-display [problemDetail]="store.allLocationsProblemDetail()"
                               [validationErrors]="store.allLocationsValidationErrors()"/>
          } @else {
            <ske-class-stock-by-category-chart [data]="store.allLocationsChart()"
                                               [selectable]="true"
                                               (barSelected)="openLocation($event)"
                                               emptyMessage="Nu există stoc înregistrat pentru această clasă."
                                               ariaLabel="Stoc pe categorii în toate locațiile. Alegeți o locație de mai jos pentru detalii."/>

            <!-- Keyboard and screen-reader alternative to clicking a chart bar. -->
            @if (locations().length) {
              <nav class="flex flex-wrap items-center gap-x-1 px-4 pb-3"
                   aria-label="Detalii stoc pe locație">
                <span nz-typography
                      nzType="secondary"
                      class="text-sm">Clic pe o bară sau alegeți locația:</span>
                @for (location of locations(); track location.id) {
                  <button nz-button
                          nzType="link"
                          nzSize="small"
                          type="button"
                          (click)="openLocation(location)">{{ location.label }}</button>
                }
              </nav>
            }
          }
        </ng-container>
      }
    </nz-card>

    <ng-template #extra>
      @if (store.drilledLocation()) {
        <button nz-button
                nzType="default"
                nzSize="small"
                type="button"
                (click)="store.closeLocation()">Înapoi la toate locațiile</button>
      }
    </ng-template>
  `
})
export class AllLocationsStockCard {
  public readonly store = inject(ClassStatisticsStore);

  public readonly title = computed(() => {
    const location = this.store.drilledLocation();
    return location ? `Stoc pe articole în ${location.name}` : 'Stoc pe categorii în toate locațiile';
  });

  /** Locations of the overview chart, as {id, label} pairs, in bar order. */
  public readonly locations = computed<ClassStockByCategoryBar[]>(() => {
    const chart = this.store.allLocationsChart();
    return chart?.labelIds.map((id, index) => ({id, label: chart.labels[index]})) ?? [];
  });

  public openLocation(bar: ClassStockByCategoryBar) {
    this.store.openLocation({id: bar.id, name: bar.label});
  }
}
