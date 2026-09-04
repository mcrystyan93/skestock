import { Component, inject } from '@angular/core';
import { Table } from './table';
import { GoodsReceiptImportListStore } from '../../services/goods-receipt-import-list.store';

@Component({
  imports: [
    Table
  ],
  selector: 'ske-goods-receipt-imports-table-container',
  styles: ``,
  templateUrl: './table-container.html',
  host: {
    class: 'absolute block inset-0'
  }
})
export class TableContainer {
  public readonly store = inject(GoodsReceiptImportListStore);
}
