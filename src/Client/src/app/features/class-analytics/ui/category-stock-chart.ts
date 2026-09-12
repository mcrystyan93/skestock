import { Component, computed, input, output } from '@angular/core';
import { CategoryStockSummaryDto } from '@ske/models';
import type { ApexAxisChartSeries, ApexChart, ApexOptions } from 'apexcharts';
import { ChartComponent } from 'ng-apexcharts';

const QUANTITY_FORMATTER = new Intl.NumberFormat('ro-RO');

@Component({
  selector: 'ske-category-stock-chart',
  imports: [ChartComponent],
  template: `
    <section class="rounded-lg bg-white p-4 shadow-sm dark:bg-gray-800" aria-labelledby="category-chart-title">
      <h2 id="category-chart-title" class="mb-3 text-lg font-semibold">Cantitate pe categorie</h2>
      <apx-chart [series]="series()"
                 [chart]="chart()"
                 [plotOptions]="plotOptions()"
                 [dataLabels]="dataLabels()"
                 [xaxis]="xaxis()"
                 [tooltip]="tooltip()"
                 aria-label="Grafic orizontal cu cantitățile din fiecare categorie"
                 role="img" />
    </section>
  `
})
export class CategoryStockChart {
  public readonly categories = input.required<CategoryStockSummaryDto[]>();
  public readonly categorySelected = output<CategoryStockSummaryDto>();

  private readonly sortedCategories = computed(() =>
    [...this.categories()].sort((first, second) => second.quantity - first.quantity)
  );

  public readonly series = computed<ApexAxisChartSeries>(() => [{
    name: 'Cantitate',
    data: this.sortedCategories().map((category) => ({ x: category.categoryName, y: category.quantity }))
  }]);

  public readonly chart = computed<ApexChart>(() => ({
    type: 'bar',
    height: 360,
    toolbar: { show: false },
    events: {
      dataPointSelection: (_event, _chart, options) => {
        const category = this.sortedCategories()[options?.dataPointIndex ?? -1];
        if (category) {
          this.categorySelected.emit(category);
        }
      }
    }
  }));

  public readonly plotOptions = computed<ApexOptions['plotOptions']>(() => ({
    bar: { horizontal: true, borderRadius: 4, distributed: true }
  }));

  public readonly dataLabels = computed<ApexOptions['dataLabels']>(() => ({
    enabled: true,
    formatter: (value) => QUANTITY_FORMATTER.format(Number(value))
  }));

  public readonly xaxis = computed<ApexOptions['xaxis']>(() => ({
    title: { text: 'Cantitate' },
    labels: { formatter: (value) => QUANTITY_FORMATTER.format(Number(value)) }
  }));

  public readonly tooltip = computed<ApexOptions['tooltip']>(() => ({
    y: { formatter: (value) => QUANTITY_FORMATTER.format(value) }
  }));
}
