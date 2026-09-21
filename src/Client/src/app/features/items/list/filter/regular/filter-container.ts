import { Component, inject } from '@angular/core';
import { GetAllItemsRequest } from '@ske/models';
import { ItemListState } from '../../../services/item-list.store';
import { FilterForm } from './filter-form';

@Component({
  imports: [
    FilterForm
  ],
  selector: 'ske-item-filter-container',
  styles: ``,
  templateUrl: './filter-container.html',
  host: {
    class: 'px-4'
  }
})
export class FilterContainer {
  public readonly store = inject(ItemListState);

  public onFilterChange(filter: GetAllItemsRequest) {
    this.store.load(filter);
  }
}
