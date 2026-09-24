import { Component, computed, inject, input } from '@angular/core';
import { ClassItemStockEvolutionDto } from '@ske/models';
import type { ApexAxisChartSeries, ApexChart, ApexOptions } from 'apexcharts';
import { ChartComponent } from 'ng-apexcharts';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { ThemeService } from '@ske/theme';

const CHART_HEIGHT = 360;
const STOCK_COLOR = '#1f6c72';
const QUANTITY_FORMATTER = new Intl.NumberFormat('ro-RO');

@Component({
  selector: 'ske-class-item-stock-evolution-chart',
  imports: [ChartComponent, NzTypographyComponent],
  template: `
    @if (hasChartData()) {
      <apx-chart [series]="series()"
                 [chart]="chart()"
                 [stroke]="stroke()"
                 [dataLabels]="dataLabels()"
                 [markers]="markers()"
                 [xaxis]="xaxis()"
                 [yaxis]="yaxis()"
                 [tooltip]="tooltip()"
                 [colors]="colors()"
                 [attr.aria-label]="ariaLabel()"
                 [theme]="{ mode: mode() }"
                 role="img" />
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
export class ClassItemStockEvolutionChart {
  public readonly data = input<ClassItemStockEvolutionDto | null>(null);
  public readonly emptyMessage = input.required<string>();
  public readonly ariaLabel = input.required<string>();

  private readonly _themeService = inject(ThemeService);

  public readonly mode = computed(() =>
    this._themeService.currentTheme() === 'dark' ? 'dark' : 'light'
  );

  public readonly hasChartData = computed(() => (this.data()?.points.length ?? 0) > 0);

  public readonly series = computed<ApexAxisChartSeries>(() => {
    const data = this.data();
    if (!data) {
      return [];
    }

    return [{
      name: `${data.itemName} (${data.unit})`,
      data: data.points.map((point) => ({
        x: Date.parse(`${point.date}T00:00:00Z`),
        y: point.cumulativeQuantity
      }))
    }];
  });

  public readonly chart = computed<ApexChart>(() => ({
    type: 'line',
    height: CHART_HEIGHT,
    toolbar: { show: false },
    zoom: { enabled: false }
  }));

  public readonly stroke = computed<ApexOptions['stroke']>(() => ({
    curve: 'smooth',
    width: 3
  }));

  public readonly dataLabels = computed<ApexOptions['dataLabels']>(() => ({
    enabled: false
  }));

  public readonly markers = computed<ApexOptions['markers']>(() => ({
    size: this.data()?.points.length === 1 ? 4 : 0,
    hover: { size: 5 }
  }));

  public readonly xaxis = computed<ApexOptions['xaxis']>(() => ({
    type: 'datetime',
    labels: {
      datetimeUTC: true,
      format: 'dd MMM'
    }
  }));

  public readonly yaxis = computed<ApexOptions['yaxis']>(() => ({
    labels: {
      formatter: (value) => QUANTITY_FORMATTER.format(value)
    }
  }));

  public readonly tooltip = computed<ApexOptions['tooltip']>(() => ({
    x: { format: 'dd MMM yyyy' },
    y: { formatter: (value) => QUANTITY_FORMATTER.format(value) }
  }));

  public readonly colors = computed<ApexOptions['colors']>(() => [STOCK_COLOR]);
}
