import { Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { form, FormField } from '@angular/forms/signals';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { ItemDropdownValue } from '@ske/models';
import { ErrorAlert } from '@ske/shared/errors';
import { ItemDropdown } from '@ske/shared/items';
import { LoaderDirective } from '@ske/shared/loader';
import { ClassStatisticsStore } from '../../../services/class-statistics.store';
import { ClassItemStockEvolutionChart } from './class-item-stock-evolution-chart';

type ItemSelectionModel = {
  item: ItemDropdownValue;
};

@Component({
  imports: [
    FormsModule,
    FormField,
    NzCardComponent,
    ItemDropdown,
    ErrorAlert,
    LoaderDirective,
    ClassItemStockEvolutionChart
  ],
  selector: 'ske-item-stock-evolution-card',
  template: `
    <nz-card class="min-h-110 flex-body has-chart"
             [nzExtra]="itemDropdownExtra"
             nzTitle="Evoluția stocului articolului selectat">
      <ng-template #itemDropdownExtra>
        <div class="w-44 sm:w-56">
          <ske-item-dropdown [formField]="itemForm.item"
                             [itemIds]="store.itemIds()"
                             [allowClear]="true"
                             [allowEdit]="false"
                             [allowCreate]="false"
                             placeholder="Alegeți un articol" />
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
                                                ariaLabel="Evoluția stocului pentru articolul selectat" />
        }
      </ng-container>
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

  private readonly _formModel = signal<ItemSelectionModel>({ item: null });
  public readonly itemForm = form(this._formModel);

  public readonly emptyMessage = computed(() => {
    if (this.itemForm.item().value()?.id) {
      return 'Nu există tranzacții de stoc pentru acest articol în această clasă.';
    }

    return this.store.itemIds().length > 0
      ? 'Selectați un articol pentru a vedea evoluția stocului.'
      : 'Nu există articole în raportul de stoc al acestei clase.';
  });

  private readonly _loadItemEvolutionEffectRef = effect(() => {
    const classId = this.classId();
    const isActive = this.active();
    const itemId = this.itemForm.item().value()?.id ?? null;
    const itemIds = this.store.itemIds();

    if (!isActive || !classId || !itemId || !itemIds.includes(itemId)) {
      untracked(() => this.store.loadItemEvolution(null));
      return;
    }

    untracked(() => this.store.loadItemEvolution({ classId, itemId }));
  });
}
