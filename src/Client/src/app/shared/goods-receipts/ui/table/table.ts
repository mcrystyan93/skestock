import { Component, input, signal } from '@angular/core';
import { BaseTable } from '@ske/shared/tables';
import { GetAllGoodsReceiptsRequest, GOODS_RECEIPT_TABLE_COLUMNS, GoodsReceiptListItemDto } from '@ske/models';
import { NzTableModule } from 'ng-zorro-antd/table';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { TableContainer as StockBatchesTableContainer } from '@ske/shared/stock-batches';

@Component({
  imports: [
    NzTableModule,
    DatePipe,
    StockBatchesTableContainer,
    CurrencyPipe,
    DecimalPipe
  ],
  selector: 'ske-goods-receipts-table',
  styles: ``,
  templateUrl: './table.html',
  host: {
    class: 'absolute block inset-0'
  }
})
export class Table extends BaseTable<GoodsReceiptListItemDto, GetAllGoodsReceiptsRequest> {
  public readonly loading = input.required<boolean>();

  // public readonly onView = output<GoodsReceiptListItemDto>();
  public readonly columns = GOODS_RECEIPT_TABLE_COLUMNS;
  public readonly expandedRows = signal<Set<string>>(new Set<string>());

  constructor() {
    super();
  }

  public toggleRow(id: string) {
    this.expandedRows.update((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(id))
        newSet.delete(id);
      else
        newSet.add(id);
      return newSet;
    });
  }
}
