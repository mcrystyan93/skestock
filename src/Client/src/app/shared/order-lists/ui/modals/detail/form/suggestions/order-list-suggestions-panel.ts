import { Component, inject, input, output } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import type { OrderCatalogItemDto, OrderListLineDto } from '@ske/models';
import { OrderListsHttp } from '@ske/shared/order-lists';
import { catalogLine } from '@ske/shared/order-lists/services';
import { NzAlertComponent } from 'ng-zorro-antd/alert';
import { NzSkeletonComponent } from 'ng-zorro-antd/skeleton';
import { OrderCatalogList } from '../../../../catalog/order-catalog-list';

@Component({
  imports: [NzAlertComponent, NzSkeletonComponent, OrderCatalogList],
  selector: 'ske-order-list-suggestions-panel',
  host: {
    class: 'relative min-h-0 -mb-6'
  },
  template: `
    <div class="absolute inset-0 flex flex-col pb-4">
      @if (catalog.isLoading()) {
        <nz-skeleton aria-busy="true"
                     [nzActive]="true"
                     [nzParagraph]="{ rows: 4 }" />
      } @else if (catalog.error()) {
        <nz-alert nzType="error"
                  nzShowIcon
                  nzMessage="Nu am putut încărca produsele." />
      } @else {
        <ske-order-catalog-list class="min-h-0 flex-1"
                                mode="add"
                                [items]="catalog.hasValue() ? catalog.value() : []"
                                [addedItemIds]="addedItemIds()"
                                (add)="addItem($event)" />
      }
    </div>
  `
})
export class OrderListSuggestionsPanel {
  public readonly classId = input.required<string>();
  public readonly addedItemIds = input.required<ReadonlySet<string>>();
  public readonly add = output<OrderListLineDto>();

  private readonly _orderListsHttp = inject(OrderListsHttp);

  protected readonly catalog = rxResource({
    params: () => this.classId(),
    stream: ({ params }) => this._orderListsHttp.getCatalog(params)
  });

  protected addItem(item: OrderCatalogItemDto) {
    this.add.emit(catalogLine(item));
  }
}
