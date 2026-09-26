import {Component, computed, inject, input, output} from '@angular/core';
import {ClassStockByCategoryChartDto} from '@ske/models';
import type {ApexAxisChartSeries, ApexChart, ApexOptions} from 'apexcharts';
import {ChartComponent} from 'ng-apexcharts';
import {NzTypographyComponent} from 'ng-zorro-antd/typography';
import {ThemeService} from '@ske/theme';

const MIN_CHART_HEIGHT = 360;
// Room per bar and for the legend/axis, so long item lists stay readable instead of squashed.
const BAR_ROW_HEIGHT = 28;
const CHART_CHROME_HEIGHT = 110;
const CATEGORY_LABEL_MAX_WIDTH = 180;
const CATEGORY_COLORS = ['#1f6c72', '#c17d2e', '#56698c', '#a45047', '#6e8251', '#806596'];
const QUANTITY_FORMATTER = new Intl.NumberFormat('ro-RO');

/** A clicked bar: the id and name of the location (or item) it represents. */
export type ClassStockByCategoryBar = { id: string; label: string };

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
                 [states]="states()"
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
  `,
  host: {
    // Global style (base.less) turns bars into pointer targets when they can be clicked.
    '[class.ske-chart-selectable]': 'selectable()'
  }
})
export class ClassStockByCategoryChart {
  public readonly data = input<ClassStockByCategoryChartDto | null>(null);
  public readonly emptyMessage = input.required<string>();
  public readonly ariaLabel = input.required<string>();
  /** When true, clicking a bar emits {@link barSelected}. */
  public readonly selectable = input(false);
  public readonly barSelected = output<ClassStockByCategoryBar>();

  private readonly _themeService = inject(ThemeService);

  public readonly mode = computed(() => this._themeService.currentTheme() === 'dark' ? 'dark' : 'light');

  public readonly hasChartData = computed(() => {
    const data = this.data();
    return !!data?.labels.length && !!data.series.length;
  });

  public readonly series = computed<ApexAxisChartSeries>(() => this.data()?.series ?? []);

  public readonly chart = computed<ApexChart>(() => ({
    type: 'bar',
    height: Math.max(MIN_CHART_HEIGHT, (this.data()?.labels.length ?? 0) * BAR_ROW_HEIGHT + CHART_CHROME_HEIGHT),
    stacked: true,
    toolbar: {show: false},
    events: {
      // ApexCharts calls this outside Angular templates; it only emits, the parent updates signals.
      dataPointSelection: (_event, _chart, options) => this.emitBar(options?.dataPointIndex ?? -1)
    }
  }));

  // Clicking a bar should not leave it visually "selected"; the parent replaces the chart instead.
  public readonly states = computed<ApexOptions['states']>(() => ({
    active: {filter: {type: 'none'}}
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

  // Only the hovered segment: drill-down bars hold a single non-zero category each.
  public readonly tooltip = computed<ApexOptions['tooltip']>(() => ({
    shared: false,
    intersect: true,
    y: {formatter: (value) => QUANTITY_FORMATTER.format(value)}
  }));

  public readonly colors = computed<ApexOptions['colors']>(() => CATEGORY_COLORS);

  private emitBar(index: number) {
    const data = this.data();
    const id = data?.labelIds[index];
    if (!this.selectable() || !data || id === undefined) {
      return;
    }

    this.barSelected.emit({id, label: data.labels[index]});
  }
}
