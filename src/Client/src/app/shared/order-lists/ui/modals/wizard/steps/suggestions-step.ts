import { Component, input, output } from '@angular/core';
import type { OrderCatalogItemDto } from '@ske/models';
import { OrderCatalogList } from '../../../catalog/order-catalog-list';
import { NzSkeletonComponent } from 'ng-zorro-antd/skeleton';

@Component({
  imports: [NzSkeletonComponent, OrderCatalogList],
  selector: 'ske-order-list-suggestions-step',
  host: {
    // Fills the modal down to its bottom edge (the -mb-6 cancels the body padding), so only the list scrolls.
    class: 'flex min-h-0 grow flex-col -mb-6'
  },
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
      <ske-order-catalog-list class="min-h-72 flex-1"
                              mode="select"
                              [items]="items()"
                              [selectedIds]="selectedIds()"
                              (selectedIdsChange)="selectedIdsChange.emit($event)" />
    }
  `
})
export class OrderListSuggestionsStep {
  public readonly items = input.required<OrderCatalogItemDto[]>();
  public readonly selectedIds = input.required<string[]>();
  public readonly loading = input(false);
  public readonly selectedIdsChange = output<string[]>();
}
