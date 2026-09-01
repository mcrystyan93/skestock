import { Component, inject } from '@angular/core';
import { GetAllItemsRequest } from '@ske/models';
import { ItemListState } from '../../services/item-list.store';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { FilterForm } from './filter-form';

@Component({
  imports: [
    NzCardComponent,
    FilterForm
  ],
  selector: 'ske-item-filter-container',
  styles: ``,
  templateUrl: './filter-container.html'
})
export class FilterContainer {
  public readonly store = inject(ItemListState);

  public onFilterChange(filter: GetAllItemsRequest) {
    this.store.load(filter);
  }
}
