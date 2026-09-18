import { Component, inject, output } from '@angular/core';
import { GetAllOrderListsRequest } from '@ske/models';
import { OrderListListStore } from '@ske/shared/order-lists';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { FilterForm } from './filter-form';

@Component({
  imports: [
    NzCardComponent,
    FilterForm
  ],
  selector: 'ske-order-list-filter-container',
  styles: ``,
  templateUrl: './filter-container.html'
})
export class FilterContainer {
  public readonly store = inject(OrderListListStore);
  public readonly onCreate = output<void>();

  public onFilterChange(filter: GetAllOrderListsRequest) {
    this.store.load(filter);
  }
}
