import { Component, computed, input, output } from '@angular/core';
import { GoodsReceiptCostPointDto } from '@ske/models';
import type { ApexAxisChartSeries, ApexChart, ApexOptions } from 'apexcharts';
import { ChartComponent } from 'ng-apexcharts';

const RON_FORMATTER = new Intl.NumberFormat('ro-RO', {
  style: 'currency',
  currency: 'RON',
  maximumFractionDigits: 2
});

@Component({
  selector: 'ske-receipt-cost-chart',
  imports: [ChartComponent],
  template: `
    <section class="overflow-x-auto rounded-lg bg-white p-4 shadow-sm dark:bg-gray-800"
             aria-labelledby="receipt-cost-chart-title">
      <h2 id="receipt-cost-chart-title" class="mb-3 text-lg font-semibold">Costul recepțiilor</h2>
      <apx-chart [series]="series()"
                 [chart]="chart()"
                 [xaxis]="xaxis()"
                 [dataLabels]="dataLabels()"
                 [tooltip]="tooltip()"
                 aria-label="Grafic cu coloane pentru costul fiecărei recepții"
                 role="img" />
    </section>
  `
})
export class ReceiptCostChart {
  public readonly points = input.required<GoodsReceiptCostPointDto[]>();
  public readonly receiptSelected = output<GoodsReceiptCostPointDto>();

  public readonly series = computed<ApexAxisChartSeries>(() => [{
    name: 'Cost',
    data: this.points().map((point) => ({
      x: new Date(point.receivedAt).getTime(),
      y: point.totalAmount
    }))
  }]);

  public readonly chart = computed<ApexChart>(() => ({
    type: 'bar',
    height: 360,
    toolbar: {
      show: true,
      tools: { download: false, selection: true, zoom: true, zoomin: true, zoomout: true, pan: true, reset: true }
    },
    zoom: { enabled: true, type: 'x', autoScaleYaxis: true },
    events: {
      dataPointSelection: (_event, _chart, options) => {
        const point = this.points()[options?.dataPointIndex ?? -1];
        if (point) {
          this.receiptSelected.emit(point);
        }
      }
    }
  }));

  public readonly xaxis = computed<ApexOptions['xaxis']>(() => ({
    type: 'datetime',
    title: { text: 'Data recepției' }
  }));

  public readonly dataLabels = computed<ApexOptions['dataLabels']>(() => ({ enabled: false }));

  public readonly tooltip = computed<ApexOptions['tooltip']>(() => ({
    y: { formatter: (value) => RON_FORMATTER.format(Number(value)) }
  }));
}
