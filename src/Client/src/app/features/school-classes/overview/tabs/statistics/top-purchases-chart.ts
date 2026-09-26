import { Component, computed, inject, input } from '@angular/core';
import { PurchaseStatisticDto } from '@ske/models';
import type { ApexAxisChartSeries, ApexChart, ApexOptions } from 'apexcharts';
import { ChartComponent } from 'ng-apexcharts';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { ThemeService } from '@ske/theme';

export type TopPurchasesMetric = 'quantity' | 'value' | 'frequency';

const BAR_HEIGHT = 32;
const MIN_CHART_HEIGHT = 160;
const METRIC_COLORS: Record<TopPurchasesMetric, string> = {
  quantity: '#1f6c72',
  value: '#d48806',
  frequency: '#531dab'
};
const QUANTITY_FORMATTER = new Intl.NumberFormat('ro-RO', { maximumFractionDigits: 2 });
const CURRENCY_FORMATTER = new Intl.NumberFormat('ro-RO', { style: 'currency', currency: 'RON' });
const DATE_FORMATTER = new Intl.DateTimeFormat('ro-RO', { day: 'numeric', month: 'short', year: 'numeric' });

@Component({
  selector: 'ske-top-purchases-chart',
  imports: [ChartComponent, NzTypographyComponent],
  template: `
    @if (items().length > 0) {
      <apx-chart [series]="series()"
                 [chart]="chart()"
                 [plotOptions]="{ bar: { horizontal: true, barHeight: '70%' } }"
                 [dataLabels]="dataLabels()"
                 [xaxis]="xaxis()"
                 [tooltip]="tooltip()"
                 [colors]="colors()"
                 [attr.aria-label]="ariaLabel()"
                 [theme]="{ mode: mode() }"
                 role="img" />
    } @else {
      <div class="flex min-h-40 items-center justify-center px-4 text-center"
           role="status">
        <p class="max-w-sm text-sm"
           nz-typography
           nzType="secondary">Nu există achiziții pentru filtrele selectate.</p>
      </div>
    }
  `
})
export class TopPurchasesChart {
  public readonly items = input<PurchaseStatisticDto[]>([]);
  public readonly metric = input.required<TopPurchasesMetric>();
  public readonly ariaLabel = input.required<string>();

  private readonly _themeService = inject(ThemeService);

  public readonly mode = computed(() =>
    this._themeService.currentTheme() === 'dark' ? 'dark' : 'light'
  );

  public readonly colors = computed(() => [METRIC_COLORS[this.metric()]]);

  public readonly series = computed<ApexAxisChartSeries>(() => {
    const metric = this.metric();
    return [{
      name: metric === 'quantity' ? 'Cantitate' : metric === 'value' ? 'Valoare' : 'Achiziții',
      data: this.items().map((item) => ({
        x: `${item.itemName} (${item.unit})`,
        y: metric === 'quantity' ? item.totalQuantity : metric === 'value' ? item.totalValue : item.purchaseCount
      }))
    }];
  });

  public readonly chart = computed<ApexChart>(() => ({
    type: 'bar',
    height: Math.max(MIN_CHART_HEIGHT, this.items().length * BAR_HEIGHT + 60),
    toolbar: { show: false },
    zoom: { enabled: false }
  }));

  public readonly dataLabels = computed<ApexOptions['dataLabels']>(() => ({
    enabled: true,
    formatter: (value) => this.formatMetric(Number(value))
  }));

  public readonly xaxis = computed<ApexOptions['xaxis']>(() => ({
    labels: { formatter: (value) => this.formatMetric(Number(value)) }
  }));

  public readonly tooltip = computed<ApexOptions['tooltip']>(() => {
    const items = this.items();
    return {
      y: {
        formatter: (_value, options) => {
          const item = items[options?.dataPointIndex ?? -1];
          if (!item) {
            return '';
          }

          return [
            `${QUANTITY_FORMATTER.format(item.totalQuantity)} ${item.unit}`,
            CURRENCY_FORMATTER.format(item.totalValue),
            `${item.purchaseCount}× (medie ${QUANTITY_FORMATTER.format(item.averageQuantity)} ${item.unit})`,
            `ultima: ${DATE_FORMATTER.format(new Date(item.lastPurchasedAt))}`
          ].join(' · ');
        }
      }
    };
  });

  private formatMetric(value: number): string {
    switch (this.metric()) {
      case 'value':
        return CURRENCY_FORMATTER.format(value);
      case 'frequency':
        return `${QUANTITY_FORMATTER.format(value)}×`;
      default:
        return QUANTITY_FORMATTER.format(value);
    }
  }
}
