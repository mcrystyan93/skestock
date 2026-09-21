import { Component, computed, inject, input, output } from '@angular/core';
import { OrderListDetailState } from '@ske/shared/order-lists';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzSpinComponent } from 'ng-zorro-antd/spin';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { LowStockItemDto, OrderListLineDto } from '@ske/models';
import { ErrorAlert } from '@ske/shared/errors';

@Component({
  imports: [
    NzRowDirective,
    NzColDirective,
    NzCardComponent,
    NzButtonComponent,
    NzIconDirective,
    NzSpinComponent,
    NzTypographyComponent,
    ErrorAlert
  ],
  selector: 'ske-order-list-low-stock-items',
  styles: ``,
  template: `
    @if (store.lowStockItemsLoading()) {
      <nz-spin nzSimple
               nzTip="Se încarcă articolele cu stoc scăzut..." />
    } @else if (store.lowStockItemsProblemDetail() || store.lowStockItemsValidationErrors()) {
      <ske-error-display [problemDetail]="store.lowStockItemsProblemDetail()"
                         [validationErrors]="store.lowStockItemsValidationErrors()" />
      @if (classId(); as currentClassId) {
        <button type="button"
                nz-button
                nzType="link"
                [disabled]="disabled()"
                (click)="store.loadLowStockItems(currentClassId)">
          Reîncearcă
        </button>
      }
    } @else if (store.lowStockItemsLoaded() && store.lowStockItemsByLocation().size === 0) {
      <p nz-typography>
        Nu există articole cu stoc scăzut pentru această clasă.
      </p>
    } @else if (store.lowStockItemsLoaded()) {
      <nz-row [nzGutter]="[16, 16]">
        @for (location of store.lowStockItemsByLocation().keys(); track location) {
          @let products = store.lowStockItemsByLocation().get(location) ?? [];
          @let allWereAdded = allItemsAdded().get(location) ?? false;
          <nz-col [nzSpan]="6">
            <nz-card [nzTitle]="location"
                     nzSize="small"
                     [nzExtra]="extra">
              <ng-template #extra>
                @if (allWereAdded) {
                  <nz-icon nzType="icons:circle-check"
                           class="text-green-800! text-lg"
                           aria-hidden="true"></nz-icon>
                } @else {
                  <button type="button"
                          nz-button
                          nzType="link"
                          [disabled]="disabled()"
                          (click)="addProducts.emit(products)">
                    <nz-icon nzType="icons:plus"
                             aria-hidden="true"></nz-icon>
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
    }
  `
})
export class OrderListLowStockItems {
  public readonly classId = input<string | null>(null);
  public readonly disabled = input(false);
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
