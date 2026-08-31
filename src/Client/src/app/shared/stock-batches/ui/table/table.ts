import { Component, input } from '@angular/core';
import { BaseTable } from '@ske/shared/tables';
import { GetAllStockBatchesRequest, STOCK_BATCH_TABLE_COLUMNS, StockBatchListItemDto } from '@ske/models';
import { NzTableModule } from 'ng-zorro-antd/table';
import { DatePipe } from '@angular/common';

@Component({
  imports: [
    NzTableModule,
    DatePipe
  ],
  selector: 'ske-stock-batches-table',
  styles: ``,
  templateUrl: './table.html',
  host: {
    class: 'absolute block inset-0'
  }
})
export class Table extends BaseTable<StockBatchListItemDto, GetAllStockBatchesRequest> {
  public readonly loading = input.required<boolean>();

  public readonly columns = STOCK_BATCH_TABLE_COLUMNS;

  constructor() {
    super();
  }
}
