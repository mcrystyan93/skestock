import { Component, computed, input, output } from '@angular/core';
import { form, FormField } from '@angular/forms/signals';
import { NzCheckboxComponent } from 'ng-zorro-antd/checkbox';
import { NzEmptyComponent } from 'ng-zorro-antd/empty';
import { NzSkeletonComponent } from 'ng-zorro-antd/skeleton';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import type { SuggestedItem } from '@ske/shared/order-lists/services';
import { checkedRowsModel, emitCheckedIdsOnChange } from './checked-rows';

@Component({
  imports: [FormField, NzCheckboxComponent, NzEmptyComponent, NzSkeletonComponent, NzTagComponent],
  selector: 'ske-order-list-suggestions-step',
  template: `
    <p class="mb-3 mt-0 opacity-80">
      Articole cu stoc scăzut în această clasă. Le poți ajusta în pasul următor.
    </p>

    @if (loading()) {
      <nz-skeleton aria-busy="true" [nzActive]="true" [nzParagraph]="{ rows: 4 }" />
    } @else if (items().length === 0) {
      <nz-empty nzNotFoundContent="Nu există articole cu stoc scăzut. Poți continua și adăuga articole manual." />
    } @else {
      <div class="mb-2 flex items-center justify-between gap-2 border-0 border-b border-solid border-gray-200 pb-2 dark:border-gray-700">
        <label nz-checkbox
               [nzChecked]="allSelected()"
               [nzIndeterminate]="partiallySelected()"
               (nzCheckedChange)="toggleAll($event)">
          Selectează tot
        </label>
        <span class="text-sm opacity-70" aria-live="polite">
          {{ selectedCount() }} din {{ items().length }} selectate
        </span>
      </div>

      <ul class="m-0 max-h-[50vh] list-none overflow-auto p-0">
        @for (row of suggestionsForm.rows; track $index) {
          @let item = items()[$index];
          @if (item) {
            <li class="flex items-start gap-2 py-2">
              <label nz-checkbox
                     class="grow"
                     [formField]="row.checked">
                <span class="font-medium">{{ item.itemName }}</span>
                @if (item.sku) {
                  <span class="ml-1 text-sm opacity-60">({{ item.sku }})</span>
                }
              </label>
              <span class="flex flex-wrap justify-end gap-1">
                @for (location of item.locations; track location) {
                  <nz-tag class="m-0">{{ location }}</nz-tag>
                }
              </span>
            </li>
          }
        }
      </ul>
    }
  `
})
export class OrderListSuggestionsStep {
  public readonly items = input.required<SuggestedItem[]>();
  public readonly selectedIds = input.required<string[]>();
  public readonly loading = input(false);
  public readonly selectedIdsChange = output<string[]>();

  private readonly _model = checkedRowsModel(() => this.items().map((item) => item.itemId), this.selectedIds);

  protected readonly suggestionsForm = form(this._model);

  protected readonly selectedCount = computed(() => this._model().rows.filter((row) => row.checked).length);
  protected readonly allSelected = computed(() =>
    this._model().rows.length > 0 && this.selectedCount() === this._model().rows.length
  );
  protected readonly partiallySelected = computed(() => this.selectedCount() > 0 && !this.allSelected());

  private readonly _emitSelection = emitCheckedIdsOnChange(this._model, this.selectedIds, (ids) => this.selectedIdsChange.emit(ids));

  protected toggleAll(checked: boolean) {
    this._model.update((model) => ({ rows: model.rows.map((row) => ({ ...row, checked })) }));
  }
}
