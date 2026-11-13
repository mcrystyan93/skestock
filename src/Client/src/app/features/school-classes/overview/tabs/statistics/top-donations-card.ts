import { Component, effect, inject, input, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { form, FormField } from '@angular/forms/signals';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { NzSegmentedComponent, NzSegmentedItemComponent } from 'ng-zorro-antd/segmented';
import { NzTabComponent, NzTabsComponent } from 'ng-zorro-antd/tabs';
import { CategoryDropdownValue, PurchaseStatisticsScope } from '@ske/models';
import { CategoryDropdown } from '@ske/shared/categories';
import { ErrorAlert } from '@ske/shared/errors';
import { LoaderDirective } from '@ske/shared/loader';
import { ClassStatisticsStore } from '../../../services/class-statistics.store';
import { TopPurchasesChart } from './top-purchases-chart';

type TopDonationsFilterModel = {
  scope: PurchaseStatisticsScope;
  category: CategoryDropdownValue;
};

@Component({
  imports: [
    FormsModule,
    FormField,
    NzCardComponent,
    NzSegmentedComponent,
    NzSegmentedItemComponent,
    NzTabsComponent,
    NzTabComponent,
    CategoryDropdown,
    ErrorAlert,
    LoaderDirective,
    TopPurchasesChart
  ],
  selector: 'ske-top-donations-card',
  template: `
    <nz-card class="min-h-110 flex-body has-chart"
             nzTitle="Top donații">
      <div class="mb-2 flex flex-wrap items-center gap-2 px-4 pt-2">
        <nz-segmented [formField]="filterForm.scope"
                      aria-label="Perioada analizată">
          @for (option of scopeOptions; track option.value) {
            <label nz-segmented-item
                   [nzValue]="option.value">{{ option.label }}</label>
          }
        </nz-segmented>
        <ske-category-dropdown class="min-w-48 grow"
                               [formField]="filterForm.category"
                               [allowClear]="true"
                               [allowEdit]="false"
                               [allowCreate]="false"
                               placeholder="Categorie"
                               aria-label="Filtrează după categorie" />
      </div>

      <ng-container *skeLoader="store.topDonationsLoading()">
        @if (store.topDonationsProblemDetail() || store.topDonationsValidationErrors()) {
          <ske-error-display [problemDetail]="store.topDonationsProblemDetail()"
                             [validationErrors]="store.topDonationsValidationErrors()" />
        } @else {
          <nz-tabs nzSize="small"
                   class="px-4!">
            <nz-tab nzTitle="Cantitate">
              <ske-top-purchases-chart [items]="store.topDonations()?.byQuantity ?? []"
                                       metric="quantity"
                                       noun="donații"
                                       ariaLabel="Articolele donate în cea mai mare cantitate" />
            </nz-tab>
            <nz-tab nzTitle="Valoare">
              <ske-top-purchases-chart [items]="store.topDonations()?.byValue ?? []"
                                       metric="value"
                                       noun="donații"
                                       ariaLabel="Articolele cu cea mai mare valoare donată" />
            </nz-tab>
            <nz-tab nzTitle="Frecvență">
              <ske-top-purchases-chart [items]="store.topDonations()?.byFrequency ?? []"
                                       metric="frequency"
                                       noun="donații"
                                       ariaLabel="Articolele donate cel mai des" />
            </nz-tab>
          </nz-tabs>
        }
      </ng-container>
    </nz-card>
  `,
  host: {
    class: 'block h-full min-w-0'
  }
})
export class TopDonationsCard {
  public readonly classId = input.required<string | null>();
  public readonly active = input(false);
  public readonly store = inject(ClassStatisticsStore);

  public readonly scopeOptions: { value: PurchaseStatisticsScope; label: string }[] = [
    { value: 'Class', label: 'Clasa curentă' },
    { value: 'Last90Days', label: '90 de zile' },
    { value: 'Last365Days', label: '365 de zile' }
  ];

  private readonly _formModel = signal<TopDonationsFilterModel>({ scope: 'Class', category: null });
  public readonly filterForm = form(this._formModel);

  private readonly _loadEffectRef = effect(() => {
    const classId = this.classId();
    const scope = this.filterForm.scope().value();
    const categoryId = this.filterForm.category().value()?.id ?? null;

    if (!this.active() || !classId) {
      return;
    }

    untracked(() => this.store.loadTopDonations({ scope, classId, categoryId }));
  });
}
