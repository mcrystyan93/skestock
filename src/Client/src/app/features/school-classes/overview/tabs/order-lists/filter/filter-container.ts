import { Component, inject } from '@angular/core';
import { GetAllOrderListsRequest } from '@ske/models';
import { OrderListListStore } from '@ske/shared/order-lists';
import { FilterForm } from './filter-form';

@Component({
  imports: [
    FilterForm
  ],
  selector: 'ske-order-list-filter-container',
  styles: ``,
  templateUrl: './filter-container.html',
  host: {
    class: 'px-4'
  }
})
export class FilterContainer {
  public readonly store = inject(OrderListListStore);

  public onFilterChange(filter: GetAllOrderListsRequest) {
    this.store.load(filter);
  }
}
