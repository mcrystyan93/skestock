import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { Component, input, output } from '@angular/core';
import type { OrderCatalogItemDto } from '@ske/models';
import { ItemThumbnail } from '@ske/shared/items';
import { wholeQuantity } from '@ske/shared/order-lists/services';
import { NzCheckboxComponent } from 'ng-zorro-antd/checkbox';
import { NzIconDirective } from 'ng-zorro-antd/icon';

/**
 * One catalog item: `select` mode ticks it (wizard step), `add` mode adds it to the order in one tap (editor panel).
 * `marked` is the ticked state in `select` mode and "already added" in `add` mode.
 */
@Component({
  imports: [DatePipe, ItemThumbnail, NgTemplateOutlet, NzCheckboxComponent, NzIconDirective],
  selector: 'ske-order-catalog-row',
  host: {
    class: 'flex items-center gap-3 py-2'
  },
  template: `
    <ske-item-thumbnail [itemId]="item().itemId"
                        [imageFileId]="item().imageFileId"
                        [large]="true" />

    @if (mode() === 'select') {
      <label nz-checkbox
             class="flex! min-w-0 grow cursor-pointer items-center! m-0! [&>span:last-child]:min-w-0 [&>span:last-child]:flex-1 [&>span:last-child]:ps-3 [&>span:last-child]:pe-0"
             [nzChecked]="marked()"
             (nzCheckedChange)="toggle.emit($event)">
        <ng-container [ngTemplateOutlet]="details" />
      </label>
    } @else {
      <div class="flex min-w-0 grow flex-col">
        <ng-container [ngTemplateOutlet]="details" />
      </div>
      @if (marked()) {
        <span class="me-2 text-sm opacity-60">Adăugat</span>
      } @else {
        <button type="button"
                class="m-0 flex size-9 shrink-0 cursor-pointer items-center justify-center rounded border-0 bg-transparent hover:bg-black/5 dark:hover:bg-white/10"
                [attr.aria-label]="'Adaugă ' + item().name"
                [attr.title]="'Adaugă ' + item().name"
                (click)="add.emit()">
          <nz-icon nzType="icons:plus"
                   aria-hidden="true"></nz-icon>
        </button>
      }
    }

    <ng-template #details>
      <span class="flex min-w-0 flex-col">
        <span class="font-medium wrap-break-word">
          {{ item().name }}
          @if (item().sku) {
            <span class="ml-1 text-sm font-normal opacity-60">({{ item().sku }})</span>
          }
        </span>
        <span class="mt-0.5 flex flex-wrap items-center gap-x-3 gap-y-0.5 text-sm opacity-80">
          @if (isLowStock()) {
            <span class="inline-flex items-center gap-1">
              <nz-icon nzType="icons:triangle-exclamation"
                       class="text-yellow-500!"
                       aria-hidden="true"></nz-icon>
              Stoc scăzut ({{ stock() }}&nbsp;{{ item().unit }})
            </span>
          }
          @if (item().isFrequent) {
            <span class="inline-flex items-center gap-1">
              <nz-icon nzType="icons:arrows-rotate"
                       class="text-primary-600 dark:text-primary-400"
                       aria-hidden="true"></nz-icon>
              Comandat frecvent
            </span>
          }
          <span>{{ item().lastQuantity === null ? 'Cantitate' : 'Ultima cantitate' }}: {{ quantity() }}&nbsp;{{ item().unit }}@if (item().lastPurchasedAt; as purchasedAt) {&ngsp;<span class="whitespace-nowrap">({{ purchasedAt | date }})</span>}</span>
        </span>
      </span>
    </ng-template>
  `
})
export class OrderCatalogRow {
  public readonly item = input.required<OrderCatalogItemDto>();
  public readonly isLowStock = input(false);
  /** Current stock at the location the row sits under; shown next to the low-stock label. */
  public readonly stock = input(0);
  public readonly mode = input.required<'select' | 'add'>();
  public readonly marked = input(false);
  public readonly toggle = output<boolean>();
  public readonly add = output<void>();

  protected quantity() {
    return wholeQuantity(this.item().lastQuantity ?? 0);
  }
}
