import { Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { form, FormField } from '@angular/forms/signals';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { NzSegmentedComponent, NzSegmentedItemComponent } from 'ng-zorro-antd/segmented';
import { CategoryDropdownValue, ItemDropdownValue, LocationDropdownValue } from '@ske/models';
import { CategoryDropdown } from '@ske/shared/categories';
import { ErrorAlert } from '@ske/shared/errors';
import { ItemDropdown } from '@ske/shared/items';
import { LoaderDirective } from '@ske/shared/loader';
import { LocationDropdown } from '@ske/shared/locations';
import { ClassStatisticsStore } from '../../../services/class-statistics.store';
import { ConsumptionPeriod } from './class-consumption-periods';
import { ClassDailyConsumptionChart } from './class-daily-consumption-chart';

type DailyConsumptionFilterModel = {
  period: ConsumptionPeriod;
  item: ItemDropdownValue;
  location: LocationDropdownValue;
  category: CategoryDropdownValue;
};

@Component({
  imports: [
    FormsModule,
    FormField,
    NzCardComponent,
    NzSegmentedComponent,
    NzSegmentedItemComponent,
    ItemDropdown,
    LocationDropdown,
    CategoryDropdown,
    ErrorAlert,
    LoaderDirective,
    ClassDailyConsumptionChart
  ],
  selector: 'ske-daily-consumption-card',
  template: `
    <nz-card class="min-h-110 flex-body has-chart"
             [nzTitle]="title()">
      <div class="px-4 pt-2">
        <nz-segmented [formField]="filterForm.period"
                      aria-label="Perioada consumului">
          @for (option of periodOptions; track option.value) {
            <label nz-segmented-item
                   [nzValue]="option.value">{{ option.label }}</label>
          }
        </nz-segmented>
      </div>
      <div class="mb-3 grid grid-cols-1 gap-2 sm:grid-cols-3 px-4 pt-2">
        <ske-item-dropdown [formField]="filterForm.item"
                           [itemIds]="store.itemIds()"
                           [allowClear]="true"
                           [allowEdit]="false"
                           [allowCreate]="false"
                           placeholder="Articol"
                           aria-label="Filtrează după articol" />
        <ske-location-dropdown [formField]="filterForm.location"
                               [allowClear]="true"
                               [loadOnInit]="active()"
                               placeholder="Locație"
                               aria-label="Filtrează după locație" />
        <ske-category-dropdown [formField]="filterForm.category"
                               [allowClear]="true"
                               [allowEdit]="false"
                               [allowCreate]="false"
                               placeholder="Categorie"
                               aria-label="Filtrează după categorie" />
      </div>

      <ng-container *skeLoader="store.dailyConsumptionLoading()">
        @if (store.dailyConsumptionProblemDetail() || store.dailyConsumptionValidationErrors()) {
          <ske-error-display [problemDetail]="store.dailyConsumptionProblemDetail()"
                             [validationErrors]="store.dailyConsumptionValidationErrors()" />
        } @else {
          <ske-class-daily-consumption-chart [data]="store.dailyConsumption()"
                                             [period]="filterForm.period().value()"
                                             [emptyMessage]="emptyMessage()" />
        }
      </ng-container>
    </nz-card>
  `,
  host: {
    class: 'block h-full min-w-0'
  }
})
export class DailyConsumptionCard {
  public readonly classId = input.required<string | null>();
  public readonly active = input(false);
  public readonly store = inject(ClassStatisticsStore);

  public readonly periodOptions: { value: ConsumptionPeriod; label: string }[] = [
    { value: 'daily', label: 'Zilnic' },
    { value: 'weekly', label: 'Săptămânal' },
    { value: 'monthly', label: 'Lunar' }
  ];

  private readonly _formModel = signal<DailyConsumptionFilterModel>({
    period: 'daily',
    item: null,
    location: null,
    category: null
  });
  public readonly filterForm = form(this._formModel);

  public readonly title = computed(() => ({
    daily: 'Consum mediu zilnic',
    weekly: 'Consum mediu săptămânal',
    monthly: 'Consum mediu lunar'
  })[this.filterForm.period().value()]);

  private readonly _itemId = computed(() => this.filterForm.item().value()?.id ?? null);
  private readonly _locationId = computed(() => this.filterForm.location().value()?.id ?? null);
  private readonly _categoryId = computed(() => this.filterForm.category().value()?.id ?? null);

  public readonly emptyMessage = computed(() =>
    this._itemId() || this._locationId() || this._categoryId()
      ? 'Nu există consum înregistrat pentru filtrele selectate.'
      : 'Nu există consum înregistrat pentru această clasă.'
  );

  // Item and category are mutually exclusive: choosing one clears the other.
  private readonly _clearCategoryOnItemEffectRef = effect(() => {
    if (this._itemId()) {
      untracked(() => this.filterForm.category().value.set(null));
    }
  });

  private readonly _clearItemOnCategoryEffectRef = effect(() => {
    if (this._categoryId()) {
      untracked(() => this.filterForm.item().value.set(null));
    }
  });

  private readonly _loadEffectRef = effect(() => {
    const classId = this.classId();
    const filter = {
      itemId: this._itemId(),
      locationId: this._locationId(),
      categoryId: this._categoryId()
    };

    if (!this.active() || !classId || (filter.itemId && filter.categoryId)) {
      return;
    }

    untracked(() => this.store.loadDailyConsumption({ classId, filter }));
  });
}
