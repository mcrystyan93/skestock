import { Component, computed, inject, input, output } from '@angular/core';
import { OrderListDetailState } from '@ske/shared/order-lists';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { LowStockItemDto, OrderListLineDto } from '@ske/models';

@Component({
  imports: [
    NzRowDirective,
    NzColDirective,
    NzCardComponent,
    NzButtonComponent,
    NzIconDirective
  ],
  selector: 'ske-order-list-low-stock-items',
  styles: ``,
  template: `
    <nz-row [nzGutter]="[16, 16]">
      @for (location of store.lowStockItemsByLocation().keys(); track $index) {
        @let products = store.lowStockItemsByLocation().get(location) ?? [];
        @let allWereAdded = allItemsAdded().get(location) ?? false;
        <nz-col [nzSpan]="4">
          <nz-card [nzTitle]="location"
                   nzSize="small"
                   [nzExtra]="extra">
            <ng-template #extra>
              @if (allWereAdded) {
                <nz-icon nzType="icons:circle-check"
                         class="text-green-800! text-lg"></nz-icon>
              } @else {
                <button type="button"
                        nz-button
                        nzType="link"
                        (click)="addProducts.emit(products)">
                  <nz-icon nzType="icons:plus"></nz-icon>
                  Adauga articolele
                </button>
              }
            </ng-template>
            @if (!allWereAdded) {
              {{ products.length }} articole cu stoc scăzut
            } @else {
              Toate articolele cu stoc scăzut au fost adăugate
            }
          </nz-card>
        </nz-col>
      }
    </nz-row>
  `
})
export class OrderListLowStockItems {
  public readonly orderListLines = input<OrderListLineDto[]>();
  public readonly addProducts = output<LowStockItemDto[]>();

  public readonly store = inject(OrderListDetailState);

  public readonly allItemsAdded = computed(() => {
    // we return a Map<locationName, boolean> indicating if all items for that location are already added to the order list
    const lowStockItemsByLocation = this.store.lowStockItemsByLocation();
    const orderListLines = this.orderListLines() ?? [];

    const result = new Map<string, boolean>();

    for (const [location, lowStockItems] of lowStockItemsByLocation.entries()) {
      const allAdded = lowStockItems.every(lowStockItem => {
        return orderListLines.some(orderListLine => orderListLine.itemId === lowStockItem.itemId);
      });
      result.set(location, allAdded);
    }

    return result;
  });
}
