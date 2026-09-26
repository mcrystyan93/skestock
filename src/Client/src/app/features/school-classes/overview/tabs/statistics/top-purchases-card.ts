import { Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { form, FormField } from '@angular/forms/signals';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { NzSegmentedComponent, NzSegmentedItemComponent } from 'ng-zorro-antd/segmented';
import { NzTabComponent, NzTabsComponent } from 'ng-zorro-antd/tabs';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { CategoryDropdownValue, PurchaseStatisticsScope } from '@ske/models';
import { CategoryDropdown } from '@ske/shared/categories';
import { ErrorAlert } from '@ske/shared/errors';
import { LoaderDirective } from '@ske/shared/loader';
import { ClassStatisticsStore } from '../../../services/class-statistics.store';
import { TopPurchasesChart } from './top-purchases-chart';

type TopPurchasesFilterModel = {
  scope: PurchaseStatisticsScope;
  category: CategoryDropdownValue;
};

const COMPUTED_AT_FORMATTER = new Intl.DateTimeFormat('ro-RO', { dateStyle: 'medium', timeStyle: 'short' });

@Component({
  imports: [
    FormsModule,
    FormField,
    NzCardComponent,
    NzSegmentedComponent,
    NzSegmentedItemComponent,
    NzTabsComponent,
    NzTabComponent,
    NzTypographyComponent,
    CategoryDropdown,
    ErrorAlert,
    LoaderDirective,
    TopPurchasesChart
  ],
  selector: 'ske-top-purchases-card',
  template: `
    <nz-card class="min-h-110 flex-body has-chart"
             nzTitle="Top achiziții">
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

      <ng-container *skeLoader="store.topPurchasesLoading()">
        @if (store.topPurchasesProblemDetail() || store.topPurchasesValidationErrors()) {
          <ske-error-display [problemDetail]="store.topPurchasesProblemDetail()"
                             [validationErrors]="store.topPurchasesValidationErrors()" />
        } @else {
          <nz-tabs nzSize="small"
                   class="px-4!">
            <nz-tab nzTitle="Cantitate">
              <ske-top-purchases-chart [items]="store.topPurchases()?.byQuantity ?? []"
                                       metric="quantity"
                                       ariaLabel="Articolele cumpărate în cea mai mare cantitate" />
            </nz-tab>
            <nz-tab nzTitle="Valoare">
              <ske-top-purchases-chart [items]="store.topPurchases()?.byValue ?? []"
                                       metric="value"
                                       ariaLabel="Articolele cu cea mai mare valoare de achiziție" />
            </nz-tab>
            <nz-tab nzTitle="Frecvență">
              <ske-top-purchases-chart [items]="store.topPurchases()?.byFrequency ?? []"
                                       metric="frequency"
                                       ariaLabel="Articolele cumpărate cel mai des" />
            </nz-tab>
          </nz-tabs>
          @if (computedAtLabel(); as computedAt) {
            <p class="px-4 pb-2 text-xs"
               nz-typography
               nzType="secondary">Actualizat: {{ computedAt }}</p>
          }
        }
      </ng-container>
    </nz-card>
  `,
  host: {
    class: 'block h-full min-w-0'
  }
})
export class TopPurchasesCard {
  public readonly classId = input.required<string | null>();
  public readonly active = input(false);
  public readonly store = inject(ClassStatisticsStore);

  public readonly scopeOptions: { value: PurchaseStatisticsScope; label: string }[] = [
    { value: 'Class', label: 'Clasa curentă' },
    { value: 'Last90Days', label: '90 de zile' },
    { value: 'Last365Days', label: '365 de zile' }
  ];

  private readonly _formModel = signal<TopPurchasesFilterModel>({ scope: 'Class', category: null });
  public readonly filterForm = form(this._formModel);

  public readonly computedAtLabel = computed(() => {
    const computedAt = this.store.topPurchases()?.computedAt;
    return computedAt ? COMPUTED_AT_FORMATTER.format(new Date(computedAt)) : null;
  });

  private readonly _loadEffectRef = effect(() => {
    const classId = this.classId();
    const scope = this.filterForm.scope().value();
    const categoryId = this.filterForm.category().value()?.id ?? null;

    if (!this.active() || !classId) {
      return;
    }

    untracked(() => this.store.loadTopPurchases({ scope, classId, categoryId }));
  });
}
