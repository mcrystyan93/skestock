import {Component, computed, inject, input} from '@angular/core';
import {ClassStockByCategoryChartDto} from '@ske/models';
import type {ApexAxisChartSeries, ApexChart, ApexOptions} from 'apexcharts';
import {ChartComponent} from 'ng-apexcharts';
import {NzTypographyComponent} from 'ng-zorro-antd/typography';
import {ThemeService} from '@ske/theme';

const CHART_HEIGHT = 360;
const CATEGORY_LABEL_MAX_WIDTH = 180;
const CATEGORY_COLORS = ['#1f6c72', '#c17d2e', '#56698c', '#a45047', '#6e8251', '#806596'];
const QUANTITY_FORMATTER = new Intl.NumberFormat('ro-RO');

@Component({
  selector: 'ske-class-stock-by-category-chart',
  imports: [ChartComponent, NzTypographyComponent],
  template: `
    @if (hasChartData()) {
      <apx-chart [series]="series()"
                 [chart]="chart()"
                 [plotOptions]="plotOptions()"
                 [dataLabels]="dataLabels()"
                 [xaxis]="xaxis()"
                 [yaxis]="yaxis()"
                 [legend]="legend()"
                 [tooltip]="tooltip()"
                 [colors]="colors()"
                 [attr.aria-label]="ariaLabel()"
                 [theme]="{ mode: mode() }"
                 role="img"/>
    } @else {
      <div class="flex min-h-80 items-center justify-center px-4 text-center"
           role="status">
        <p class="max-w-sm text-sm"
           nz-typography
           nzType="secondary">{{ emptyMessage() }}</p>
      </div>
    }
  `
})
export class ClassStockByCategoryChart {
  public readonly data = input<ClassStockByCategoryChartDto | null>(null);
  public readonly emptyMessage = input.required<string>();
  public readonly ariaLabel = input.required<string>();

  private readonly _themeService = inject(ThemeService);

  public readonly mode = computed(() => this._themeService.currentTheme() === 'dark' ? 'dark' : 'light');

  public readonly hasChartData = computed(() => {
    const data = this.data();
    return !!data?.labels.length && !!data.series.length;
  });

  public readonly series = computed<ApexAxisChartSeries>(() => this.data()?.series ?? []);

  public readonly chart = computed<ApexChart>(() => ({
    type: 'bar',
    height: CHART_HEIGHT,
    stacked: true,
    toolbar: {show: false}
  }));

  public readonly plotOptions = computed<ApexOptions['plotOptions']>(() => ({
    bar: {
      horizontal: true,
      borderRadius: 3,
      barHeight: '65%'
    }
  }));

  public readonly dataLabels = computed<ApexOptions['dataLabels']>(() => ({
    enabled: false
  }));

  public readonly xaxis = computed<ApexOptions['xaxis']>(() => ({
    categories: this.data()?.labels ?? [],
    labels: {
      formatter: (value) => QUANTITY_FORMATTER.format(Number(value))
    }
  }));

  public readonly yaxis = computed<ApexOptions['yaxis']>(() => ({
    labels: {maxWidth: CATEGORY_LABEL_MAX_WIDTH}
  }));

  public readonly legend = computed<ApexOptions['legend']>(() => ({
    position: 'bottom',
    horizontalAlign: 'left'
  }));

  public readonly tooltip = computed<ApexOptions['tooltip']>(() => ({
    y: {formatter: (value) => QUANTITY_FORMATTER.format(value)}
  }));

  public readonly colors = computed<ApexOptions['colors']>(() => CATEGORY_COLORS);
}
