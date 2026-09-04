import { Component, inject } from '@angular/core';
import { GetAllGoodsReceiptImportsRequest } from '@ske/models';
import { GoodsReceiptImportListStore } from '../../../services/goods-receipt-import-list.store';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { FilterForm } from './filter-form';

@Component({
  imports: [
    NzCardComponent,
    FilterForm
  ],
  selector: 'ske-goods-receipt-imports-filter-container',
  styles: ``,
  templateUrl: './filter-container.html'
})
export class FilterContainer {
  public readonly store = inject(GoodsReceiptImportListStore);

  public onFilterChange(filter: GetAllGoodsReceiptImportsRequest) {
    this.store.load(filter);
  }
}
