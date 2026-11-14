import { Component, input, output } from '@angular/core';
import type { OrderCatalogItemDto } from '@ske/models';
import { OrderCatalogList } from '../../../catalog/order-catalog-list';
import { NzSkeletonComponent } from 'ng-zorro-antd/skeleton';

@Component({
  imports: [NzSkeletonComponent, OrderCatalogList],
  selector: 'ske-order-list-suggestions-step',
  template: `
    <p class="mb-3 mt-0 opacity-80">
      Bifează produsele pentru comandă; vor avea ultima cantitate achiziționată. Le poți ajusta în pasul următor.
    </p>

    @if (loading()) {
      <nz-skeleton aria-busy="true" [nzActive]="true" [nzParagraph]="{ rows: 4 }" />
    } @else {
      <div class="mb-2 text-sm opacity-70"
           aria-live="polite">
        {{ selectedIds().length }} selectate
      </div>
      <div class="flex h-[55dvh] min-h-72 flex-col">
        <ske-order-catalog-list class="min-h-0 flex-1"
                                mode="select"
                                [items]="items()"
                                [selectedIds]="selectedIds()"
                                (selectedIdsChange)="selectedIdsChange.emit($event)" />
      </div>
    }
  `
})
export class OrderListSuggestionsStep {
  public readonly items = input.required<OrderCatalogItemDto[]>();
  public readonly selectedIds = input.required<string[]>();
  public readonly loading = input(false);
  public readonly selectedIdsChange = output<string[]>();
}
