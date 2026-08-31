import { Component, inject } from '@angular/core';
import { GetClassLocationStockRequest } from '@ske/models';
import { StockStore } from '../../../services/stock.store';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { FilterForm } from './filter-form';

@Component({
  imports: [
    NzCardComponent,
    FilterForm
  ],
  selector: 'ske-stock-filter-container',
  styles: ``,
  templateUrl: './filter-container.html'
})
export class FilterContainer {
  public readonly store = inject(StockStore);

  public onFilterChange(filter: GetClassLocationStockRequest) {
    this.store.load(filter);
  }
}
