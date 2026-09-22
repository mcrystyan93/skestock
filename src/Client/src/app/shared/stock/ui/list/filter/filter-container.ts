import { Component, inject } from '@angular/core';
import { GetClassLocationStockRequest } from '@ske/models';
import { StockStore } from '../../../services/stock.store';
import { FilterForm } from './filter-form';
import { NzModalService } from 'ng-zorro-antd/modal';

@Component({
  imports: [
    FilterForm
  ],
  selector: 'ske-stock-filter-container',
  styles: ``,
  templateUrl: './filter-container.html',
  providers: [NzModalService],
  host:{
    class: 'px-4'
  }
})
export class FilterContainer {
  public readonly store = inject(StockStore);
  private readonly _nzModalService = inject(NzModalService);

  public onFilterChange(filter: GetClassLocationStockRequest) {
    this.store.load(filter);
  }
}
