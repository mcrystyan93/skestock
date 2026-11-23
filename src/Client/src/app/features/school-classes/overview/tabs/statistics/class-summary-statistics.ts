import { Component, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { ClassStatisticsHttp } from '@ske/shared/class-statistics';
import { NzAlertComponent } from 'ng-zorro-antd/alert';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { NzStatisticComponent } from 'ng-zorro-antd/statistic';

const RON_FORMATTER = new Intl.NumberFormat('ro-RO', {
  style: 'currency',
  currency: 'RON',
  maximumFractionDigits: 2
});

@Component({
  selector: 'ske-class-summary-statistics',
  imports: [NzAlertComponent, NzCardComponent, NzColDirective, NzRowDirective, NzStatisticComponent],
  template: `
    @if (summary.error()) {
      <nz-alert nzType="error"
                nzShowIcon
                nzMessage="Nu s-au putut încărca totalurile."/>
    } @else {
      <nz-row [nzGutter]="[16, 16]">
        <nz-col [nzXs]="12" [nzLg]="8">
          <nz-card nzSize="small">
            <nz-statistic nzTitle="Valoare comenzi"
                            [nzLoading]="summary.isLoading()"
                            [nzValueTemplate]="money"
                            [nzValue]="summary.value()?.totalOrders ?? 0"/>
          </nz-card>
        </nz-col>
        <nz-col [nzXs]="12" [nzLg]="8">
          <nz-card nzSize="small">
            <nz-statistic nzTitle="Număr de comenzi"
                            [nzLoading]="summary.isLoading()"
                            [nzValue]="summary.value()?.orderCount ?? 0"/>
          </nz-card>
        </nz-col>
        <nz-col [nzXs]="12" [nzLg]="8">
          <nz-card nzSize="small">
            <nz-statistic nzTitle="Total donații"
                            [nzLoading]="summary.isLoading()"
                            [nzValueTemplate]="money"
                            [nzValue]="summary.value()?.totalDonations ?? 0"/>
          </nz-card>
        </nz-col>
        <nz-col [nzXs]="12" [nzLg]="8">
          <nz-card nzSize="small">
            <nz-statistic nzTitle="Număr de donații"
                            [nzLoading]="summary.isLoading()"
                            [nzValue]="summary.value()?.donationCount ?? 0"/>
          </nz-card>
        </nz-col>
        <nz-col [nzXs]="12" [nzLg]="8">
          <nz-card nzSize="small">
            <nz-statistic nzTitle="Total cheltuieli"
                            [nzLoading]="summary.isLoading()"
                            [nzValueTemplate]="money"
                            [nzValue]="summary.value()?.totalExpenses ?? 0"/>
          </nz-card>
        </nz-col>
        <nz-col [nzXs]="24" [nzLg]="8">
          <nz-card nzSize="small">
            <nz-statistic nzTitle="Valoarea curentă a stocului"
                            [nzLoading]="summary.isLoading()"
                            [nzValueTemplate]="money"
                            [nzValue]="summary.value()?.currentStockValue ?? 0"/>
          </nz-card>
        </nz-col>
      </nz-row>
    }

    <ng-template #money let-value>{{ format(value) }}</ng-template>
  `
})
export class ClassSummaryStatistics {
  public readonly classId = input.required<string | null>();
  public readonly active = input(false);

  private readonly _http = inject(ClassStatisticsHttp);

  protected readonly summary = rxResource({
    params: () => (this.active() ? this.classId() : null) ?? undefined,
    stream: ({ params }) => this._http.getSummary(params)
  });

  protected format(value: string | number) {
    return RON_FORMATTER.format(Number(value));
  }
}
