import { Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { form, FormField } from '@angular/forms/signals';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { ItemDropdownValue, LocationDropdownValue } from '@ske/models';
import { ErrorAlert } from '@ske/shared/errors';
import { ItemDropdown } from '@ske/shared/items';
import { LoaderDirective } from '@ske/shared/loader';
import { LocationDropdown } from '@ske/shared/locations';
import { ClassStatisticsStore } from '../../../services/class-statistics.store';
import { ClassItemStockEvolutionChart } from './class-item-stock-evolution-chart';
import { LayoutBreakpoint } from '@ske/shared/directives';

type ItemSelectionModel = {
  item: ItemDropdownValue;
  location: LocationDropdownValue;
};

@Component({
  imports: [
    FormsModule,
    FormField,
    NzCardComponent,
    ItemDropdown,
    LocationDropdown,
    ErrorAlert,
    LoaderDirective,
    ClassItemStockEvolutionChart,
    LayoutBreakpoint,
    NgTemplateOutlet
  ],
  selector: 'ske-item-stock-evolution-card',
  template: `
    <nz-card class="min-h-110 flex-body has-chart"
             [nzExtra]="filtersExtra"
             nzTitle="Evoluția stocului">
      <ng-container *skeLayoutBreakpoint="'lg'; else mobileFilters"></ng-container>
      <ng-template #mobileFilters>
        <div class="px-4 pt-2">
          <ng-container *ngTemplateOutlet="filters" />
        </div>
      </ng-template>

      <ng-container *skeLoader="store.itemIdsLoading() || store.itemEvolutionLoading()">
        @if (store.itemIdsProblemDetail() || store.itemIdsValidationErrors() ||
        store.itemEvolutionProblemDetail() || store.itemEvolutionValidationErrors()) {
          <ske-error-display [problemDetail]="store.itemIdsProblemDetail() ?? store.itemEvolutionProblemDetail()"
                             [validationErrors]="store.itemIdsValidationErrors() ?? store.itemEvolutionValidationErrors()" />
        } @else {
          <ske-class-item-stock-evolution-chart [data]="store.itemEvolution()"
                                                [emptyMessage]="emptyMessage()"
                                                ariaLabel="Evoluția stocului" />
        }
      </ng-container>

      <ng-template #filtersExtra>
        <ng-container *skeLayoutBreakpoint="'lg'; else noDesktopFilters">
          <div class="w-96">
            <ng-container *ngTemplateOutlet="filters" />
          </div>
        </ng-container>
        <ng-template #noDesktopFilters></ng-template>
      </ng-template>

      <ng-template #filters>
        <div class="grid grid-cols-1 gap-2 sm:grid-cols-2">
          <ske-item-dropdown [formField]="itemForm.item"
                             [itemIds]="store.itemIds()"
                             [allowClear]="true"
                             [allowEdit]="false"
                             [allowCreate]="false"
                             placeholder="Articol"
                             aria-label="Filtrează după articol" />
          <ske-location-dropdown [formField]="itemForm.location"
                                 [allowClear]="true"
                                 [loadOnInit]="active()"
                                 placeholder="Locație"
                                 aria-label="Filtrează după locație" />
        </div>
      </ng-template>
    </nz-card>
  `,
  host: {
    class: 'block h-full min-w-0'
  }
})
export class ItemStockEvolutionCard {
  public readonly classId = input.required<string | null>();
  public readonly active = input(false);
  public readonly store = inject(ClassStatisticsStore);

  private readonly _formModel = signal<ItemSelectionModel>({ item: null, location: null });
  public readonly itemForm = form(this._formModel);

  public readonly emptyMessage = computed(() => {
    return this.itemForm.item().value()?.id || this.itemForm.location().value()?.id
      ? 'Nu există tranzacții de stoc pentru filtrele selectate.'
      : 'Nu există tranzacții de stoc în această clasă.';
  });

  private readonly _loadItemEvolutionEffectRef = effect(() => {
    const classId = this.classId();
    const isActive = this.active();
    const itemId = this.itemForm.item().value()?.id ?? null;
    const locationId = this.itemForm.location().value()?.id ?? null;

    if (!isActive || !classId) {
      untracked(() => this.store.loadItemEvolution(null));
      return;
    }

    untracked(() => this.store.loadItemEvolution({ classId, filter: { itemId, locationId } }));
  });
}
