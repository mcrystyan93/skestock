import { Component, computed, inject, input } from '@angular/core';
import { ClassDailyConsumptionDto } from '@ske/models';
import type { ApexAxisChartSeries, ApexChart, ApexOptions } from 'apexcharts';
import { ChartComponent } from 'ng-apexcharts';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { ThemeService } from '@ske/theme';

const CHART_HEIGHT = 320;
const USAGE_COLOR = '#1f6c72';
const AVERAGE_COLOR = '#d4380d';
const QUANTITY_FORMATTER = new Intl.NumberFormat('ro-RO', { maximumFractionDigits: 2 });
const CURRENCY_FORMATTER = new Intl.NumberFormat('ro-RO', { style: 'currency', currency: 'RON' });

@Component({
  selector: 'ske-class-daily-consumption-chart',
  imports: [ChartComponent, NzTypographyComponent],
  template: `
    @if (hasChartData()) {
      <p class="mb-2 text-sm px-4"
         nz-typography
         nzType="secondary">
        Medie zilnică: <strong>{{ averageQuantityLabel() }}</strong> buc. ·
        <strong>{{ averageValueLabel() }}</strong>
      </p>
      <apx-chart [series]="series()"
                 [chart]="chart()"
                 [dataLabels]="{ enabled: false }"
                 [plotOptions]="plotOptions()"
                 [xaxis]="xaxis()"
                 [yaxis]="yaxis()"
                 [tooltip]="tooltip()"
                 [annotations]="annotations()"
                 [colors]="colors"
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
export class ClassDailyConsumptionChart {
  public readonly data = input<ClassDailyConsumptionDto | null>(null);
  public readonly emptyMessage = input.required<string>();
  public readonly ariaLabel = input.required<string>();

  private readonly _themeService = inject(ThemeService);

  public readonly colors = [USAGE_COLOR];

  public readonly mode = computed(() =>
    this._themeService.currentTheme() === 'dark' ? 'dark' : 'light'
  );

  public readonly hasChartData = computed(() => (this.data()?.totalQuantity ?? 0) > 0);

  public readonly averageQuantityLabel = computed(() =>
    QUANTITY_FORMATTER.format(this.data()?.averageQuantity ?? 0)
  );

  public readonly averageValueLabel = computed(() =>
    CURRENCY_FORMATTER.format(this.data()?.averageValue ?? 0)
  );

  public readonly series = computed<ApexAxisChartSeries>(() => [{
    name: 'Consum',
    data: (this.data()?.points ?? []).map((point) => ({
      x: Date.parse(`${point.date}T00:00:00Z`),
      y: point.quantity
    }))
  }]);

  public readonly chart = computed<ApexChart>(() => ({
    type: 'bar',
    height: CHART_HEIGHT,
    toolbar: { show: false },
    zoom: { enabled: false }
  }));

  public readonly plotOptions = computed<ApexOptions['plotOptions']>(() => ({
    bar: { columnWidth: '80%' }
  }));

  public readonly xaxis = computed<ApexOptions['xaxis']>(() => ({
    type: 'datetime',
    labels: { datetimeUTC: true, format: 'dd MMM' }
  }));

  public readonly yaxis = computed<ApexOptions['yaxis']>(() => ({
    labels: { formatter: (value) => QUANTITY_FORMATTER.format(value) }
  }));

  public readonly tooltip = computed<ApexOptions['tooltip']>(() => {
    const points = this.data()?.points ?? [];
    return {
      x: { format: 'dd MMM yyyy' },
      y: {
        formatter: (value, options) => {
          const point = points[options?.dataPointIndex ?? -1];
          const quantity = QUANTITY_FORMATTER.format(value);
          return point ? `${quantity} (${CURRENCY_FORMATTER.format(point.value)})` : quantity;
        }
      }
    };
  });

  public readonly annotations = computed<ApexOptions['annotations']>(() => ({
    yaxis: [{
      y: this.data()?.averageQuantity ?? 0,
      borderColor: AVERAGE_COLOR,
      strokeDashArray: 4,
      label: {
        text: `Medie ${this.averageQuantityLabel()}`,
        borderColor: AVERAGE_COLOR,
        style: { color: '#fff', background: AVERAGE_COLOR }
      }
    }]
  }));
}
