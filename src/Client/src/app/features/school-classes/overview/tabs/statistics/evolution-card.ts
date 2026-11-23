import { Component, computed, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { ThemeService } from '@ske/theme';
import { ClassStatisticsHttp } from '@ske/shared/class-statistics';
import type { ApexAxisChartSeries, ApexChart, ApexOptions } from 'apexcharts';
import { ChartComponent } from 'ng-apexcharts';
import { NzAlertComponent } from 'ng-zorro-antd/alert';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { NzSkeletonComponent } from 'ng-zorro-antd/skeleton';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { CHART_AXIS_CHROME, CHART_FONT, CHART_GRID } from './chart-style';

const RON_FORMATTER = new Intl.NumberFormat('ro-RO', {
  style: 'currency',
  currency: 'RON',
  maximumFractionDigits: 2
});
const AXIS_FORMATTER = new Intl.NumberFormat('ro-RO', { notation: 'compact', maximumFractionDigits: 1 });

@Component({
  selector: 'ske-evolution-card',
  imports: [
    ChartComponent,
    NzAlertComponent,
    NzCardComponent,
    NzSkeletonComponent,
    NzTypographyComponent
  ],
  template: `
    <nz-card nzTitle="Evoluție">
      @if (evolution.error()) {
        <nz-alert nzType="error"
                  nzShowIcon
                  nzMessage="Nu s-a putut încărca evoluția."/>
      } @else if (evolution.isLoading()) {
        <nz-skeleton [nzActive]="true"/>
      } @else if (points().length > 0) {
        <apx-chart [series]="series()"
                   [chart]="chart"
                   [stroke]="stroke"
                   [grid]="grid"
                   [colors]="colors"
                   [markers]="markers"
                   [dataLabels]="dataLabels"
                   [legend]="legend"
                   [xaxis]="xaxis()"
                   [yaxis]="yaxis"
                   [tooltip]="tooltip"
                   [theme]="{ mode: mode() }"
                   role="img"
                   aria-label="Evoluția comenzilor, donațiilor și cheltuielilor"/>
      } @else {
        <div class="flex min-h-40 items-center justify-center px-4 text-center"
             role="status">
          <p class="text-sm"
             nz-typography
             nzType="secondary">Nu există încă intrări pentru această clasă.</p>
        </div>
      }
    </nz-card>
  `
})
export class EvolutionCard {
  public readonly classId = input.required<string | null>();
  public readonly active = input(false);

  private readonly _http = inject(ClassStatisticsHttp);
  private readonly _themeService = inject(ThemeService);

  protected readonly evolution = rxResource({
    params: () => (this.active() ? this.classId() : null) ?? undefined,
    stream: ({ params }) => this._http.getEvolution(params)
  });

  protected readonly mode = computed(() =>
    this._themeService.currentTheme() === 'dark' ? 'dark' : 'light'
  );

  protected readonly points = computed(() => this.evolution.value()?.points ?? []);

  protected readonly series = computed<ApexAxisChartSeries>(() => {
    const points = this.points();
    // Running total: each month adds to everything before it, so the lines only go up.
    const toData = (pick: (point: (typeof points)[number]) => number) => {
      let total = 0;
      return points.map((point) => ({
        x: Date.parse(`${point.weekStart}T00:00:00Z`),
        y: Math.round((total += pick(point)) * 100) / 100
      }));
    };

    return [
      { name: 'Comenzi', data: toData((p) => p.orders) },
      { name: 'Donații', data: toData((p) => p.donations) },
      { name: 'Cheltuieli (punctat)', data: toData((p) => p.expenses) }
    ];
  });

  protected readonly chart: ApexChart = {
    type: 'line',
    height: 340,
    fontFamily: CHART_FONT,
    toolbar: { show: false },
    zoom: { enabled: false }
  };

  protected readonly stroke: ApexOptions['stroke'] = {
    curve: 'smooth',
    width: 2.5,
    dashArray: [0, 0, 6]
  };

  protected readonly grid = CHART_GRID;
  protected readonly colors = ['#1677ff', '#52c41a', '#fa8c16'];
  protected readonly markers: ApexOptions['markers'] = { size: 4, strokeWidth: 0, hover: { size: 6 } };
  protected readonly dataLabels: ApexOptions['dataLabels'] = { enabled: false };
  protected readonly legend: ApexOptions['legend'] = { position: 'top', horizontalAlign: 'left' };

  protected readonly xaxis = computed<ApexOptions['xaxis']>(() => ({
    type: 'datetime',
    // Capped ticks keep long histories readable.
    tickAmount: Math.min(Math.max(this.points().length - 1, 1), 10),
    ...CHART_AXIS_CHROME,
    labels: { datetimeUTC: true, format: 'dd MMM yy' }
  }));

  protected readonly yaxis: ApexOptions['yaxis'] = {
    min: 0,
    labels: { formatter: (value) => AXIS_FORMATTER.format(value) }
  };

  protected readonly tooltip: ApexOptions['tooltip'] = {
    shared: true,
    x: { format: "'Săpt. din' dd MMM yyyy" },
    y: { formatter: (value) => RON_FORMATTER.format(value ?? 0) }
  };
}
