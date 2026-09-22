import { Component, effect, input, output, signal, untracked } from '@angular/core';
import { BaseTableWithFilter } from '@ske/shared/tables';
import { GetAllGoodsReceiptsRequest, GOODS_RECEIPT_TABLE_COLUMNS, GoodsReceiptListItemDto } from '@ske/models';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { TableContainer as StockBatchesTableContainer } from '@ske/shared/stock-batches';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { TimeAgoPipe } from '@ske/shared/pipes';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NzTooltipDirective } from 'ng-zorro-antd/tooltip';

@Component({
  imports: [
    NzTableModule,
    NzButtonComponent,
    NzIconDirective,
    DatePipe,
    StockBatchesTableContainer,
    CurrencyPipe,
    DecimalPipe,
    NzTypographyComponent,
    TimeAgoPipe,
    NzTagComponent,
    NzTooltipDirective
  ],
  selector: 'ske-goods-receipts-table',
  styles: ``,
  templateUrl: './table.html',
  host: {
    class: 'absolute block inset-0'
  }
})
export class Table extends BaseTableWithFilter<GoodsReceiptListItemDto, GetAllGoodsReceiptsRequest> {
  public readonly loading = input.required<boolean>();
  public readonly expandedReceiptId = input<string | null>(null);

  public readonly downloadFile = output<GoodsReceiptListItemDto>();
  public readonly columns = GOODS_RECEIPT_TABLE_COLUMNS;
  public readonly expandedRows = signal<Set<string>>(new Set<string>());

  private readonly _expandedReceiptEffect = effect(() => {
    const receiptId = this.expandedReceiptId();
    if (!receiptId || !this.items().some((item) => item.id === receiptId)) {
      return;
    }

    untracked(() => this.expandedRows.set(new Set([receiptId])));
  });

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
