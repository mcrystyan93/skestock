import {Component, computed, input} from '@angular/core';
import {SchoolClassGoodsReceiptSummary} from '@ske/models';
import type {ApexAxisChartSeries, ApexChart, ApexOptions} from 'apexcharts';
import {ChartComponent} from 'ng-apexcharts';
import {NzStatisticComponent} from 'ng-zorro-antd/statistic';

const RON_FORMATTER = new Intl.NumberFormat('ro-RO', {
  style: 'currency',
  currency: 'RON',
  maximumFractionDigits: 2
});

@Component({
  selector: 'ske-goods-receipt-cost-statistic',
  imports: [NzStatisticComponent, ChartComponent],
  template: `
    <nz-statistic nzTitle="Evolutia comenzilor"
                  [nzValueTemplate]="chartTemplate">
      <ng-template #chartTemplate>
        @if (receipts().length > 0) {
          <apx-chart [series]="series()"
                     [chart]="chart()"
                     [stroke]="stroke()"
                     [markers]="markers()"
                     [xaxis]="xaxis()"
                     [yaxis]="yaxis()"
                     [dataLabels]="dataLabels()"
                     [tooltip]="tooltip()"
                     role="img"
                     aria-label="Evoluția valorii comenzilor în timp"/>
        } @else {
          <span aria-label="Nu există comenzi">—</span>
        }
      </ng-template>
    </nz-statistic>
  `
})
export class GoodsReceiptCostStatistic {
  public readonly receipts = input<SchoolClassGoodsReceiptSummary[]>([]);

  public readonly series = computed<ApexAxisChartSeries>(() => [{
    data: this.receipts().map((receipt) => ({
      x: new Date(receipt.receivedAt).getTime(),
      y: receipt.totalAmount
    }))
  }]);

  public readonly chart = computed<ApexChart>(() => ({
    type: 'line',
    height: 35,
    width: 150,
    sparkline: {enabled: true},
    toolbar: {show: false},
    zoom: {enabled: false},
    // events: {
    //   mouseMove: (event, chartContext, config) => {
    //     if (!chartContext || !config || config.seriesIndex < 0) {
    //       return;
    //     }
    //
    //     const chartElement = config['dom']?.baseEl;
    //
    //     if (!chartElement ||
    //         typeof chartElement.querySelector !== 'function' ||
    //         typeof chartElement.getBoundingClientRect !== 'function') {
    //       return;
    //     }
    //
    //     const chartBounds = chartElement.getBoundingClientRect();
    //     const left = event.clientX - chartBounds.left;
    //     const top = event.clientY - chartBounds.top;
    //
    //     if (typeof left !== 'number' || typeof top !== 'number') {
    //       return;
    //     }
    //
    //     const tooltip: HTMLElement | null = chartElement.querySelector('.apexcharts-tooltip');
    //
    //     if (!tooltip) {
    //       return;
    //     }
    //
    //     tooltip.style.left = `${left}px`;
    //     tooltip.style.top = `${top}px`;
    //     tooltip.style.transform = 'translate(-50%, calc(-100% - 8px))';
    //   }
    // }
  }));

  public readonly stroke = computed<ApexOptions['stroke']>(() => ({
    curve: 'straight',
    width: 2
  }));

  public readonly markers = computed<ApexOptions['markers']>(() => ({
    size: 3,
    strokeWidth: 0,
    hover: {size: 5}
  }));

  public readonly xaxis = computed<ApexOptions['xaxis']>(() => ({
    type: 'datetime',
    labels: {show: false},
    axisBorder: {show: false},
    axisTicks: {show: false}
  }));

  public readonly yaxis = computed<ApexOptions['yaxis']>(() => ({
    show: false,
    labels: {show: false}
  }));

  public readonly dataLabels = computed<ApexOptions['dataLabels']>(() => ({
    enabled: false
  }));

  public readonly tooltip = computed<ApexOptions['tooltip']>(() => ({
    fixed: {
      enabled: false
    },
    marker: {show: false},
    x: {format: 'dd MMM yyyy', show:false},
    y: {
      title: {
        formatter: (_) => {
          return ''
        }
      },
      formatter: (value) => RON_FORMATTER.format(Number(value))
    }
  }));
}
