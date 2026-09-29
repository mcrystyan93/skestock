import { Component, inject } from '@angular/core';
import { GetAllGoodsReceiptImportsRequest } from '@ske/models';
import { GoodsReceiptImportListStore } from '../../../services/goods-receipt-import-list.store';
import { FilterForm } from './filter-form';

@Component({
  imports: [
    FilterForm
  ],
  selector: 'ske-goods-receipt-imports-filter-container',
  styles: ``,
  templateUrl: './filter-container.html',
  host:{
    class: 'px-4'
  }
})
export class FilterContainer {
  public readonly store = inject(GoodsReceiptImportListStore);

  public onFilterChange(filter: GetAllGoodsReceiptImportsRequest) {
    this.store.load(filter);
  }
}
