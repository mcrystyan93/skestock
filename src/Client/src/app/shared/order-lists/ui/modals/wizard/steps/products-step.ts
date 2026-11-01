import { Component, computed, input, output } from '@angular/core';
import { form, FormField } from '@angular/forms/signals';
import { ItemThumbnail } from '@ske/shared/items';
import type { ReviewedProduct } from '@ske/shared/order-lists/services';
import { NzCheckboxComponent } from 'ng-zorro-antd/checkbox';
import { NzEmptyComponent } from 'ng-zorro-antd/empty';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { checkedRowsModel, emitCheckedIdsOnChange } from './checked-rows';

@Component({
  imports: [FormField, ItemThumbnail, NzCheckboxComponent, NzEmptyComponent, NzTagComponent],
  selector: 'ske-order-list-products-step',
  template: `
    <p class="mb-3 mt-0 opacity-80">
      Debifează produsele pe care nu le vrei în comandă. Cantitățile le poți modifica în ultimul pas.
    </p>

    @if (products().length === 0) {
      <nz-empty nzNotFoundContent="Listele selectate nu conțin produse." />
    } @else {
      <div class="mb-2 flex items-center justify-between gap-2 border-0 border-b border-solid border-gray-200 pb-2 dark:border-gray-700">
        <label nz-checkbox
               [nzChecked]="allSelected()"
               [nzIndeterminate]="partiallySelected()"
               (nzCheckedChange)="toggleAll($event)">
          Selectează tot
        </label>
        <span class="text-sm opacity-70" aria-live="polite">
          {{ selectedCount() }} din {{ products().length }} selectate
        </span>
      </div>

      <ul class="m-0 max-h-[50vh] list-none overflow-auto p-0">
        @for (row of productsForm.rows; track $index) {
          @let product = products()[$index];
          @if (product) {
            <li class="flex items-center gap-3 py-2">
              <label nz-checkbox [formField]="row.checked"><span class="sr-only">{{ product.productName }}</span></label>
              <ske-item-thumbnail [itemId]="product.itemId"
                                  [imageFileId]="product.imageFileId" />
              <span class="flex min-w-0 grow flex-col">
                <span class="truncate font-medium">{{ product.productName }}</span>
                <span class="text-sm opacity-70">{{ product.quantity }} {{ product.unit }}</span>
              </span>
              <span class="flex flex-wrap justify-end gap-1">
                @for (name of product.listNames; track name) {
                  <nz-tag class="m-0">{{ name }}</nz-tag>
                }
              </span>
            </li>
          }
        }
      </ul>
    }
  `
})
export class OrderListProductsStep {
  public readonly products = input.required<ReviewedProduct[]>();
  public readonly selectedIds = input.required<string[]>();
  public readonly selectedIdsChange = output<string[]>();

  private readonly _model = checkedRowsModel(() => this.products().map((product) => product.itemId), this.selectedIds);

  protected readonly productsForm = form(this._model);

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
