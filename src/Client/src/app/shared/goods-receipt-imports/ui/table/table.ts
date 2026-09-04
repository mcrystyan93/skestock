import { Component, input } from '@angular/core';
import { BaseTable } from '@ske/shared/tables';
import {
  GetAllGoodsReceiptImportsRequest, GOODS_RECEIPT_IMPORT_STATUS_COLORS,
  GOODS_RECEIPT_IMPORT_STATUS_LABELS, GOODS_RECEIPT_IMPORT_TABLE_COLUMNS, GoodsReceiptImportListItemDto,
  GoodsReceiptImportStatus
} from '@ske/models';
import { NzTableModule } from 'ng-zorro-antd/table';
import { DatePipe } from '@angular/common';
import { NzTagComponent } from 'ng-zorro-antd/tag';

@Component({
  imports: [
    NzTableModule,
    DatePipe,
    NzTagComponent
  ],
  selector: 'ske-goods-receipt-imports-table',
  styles: ``,
  templateUrl: './table.html',
  host: {
    class: 'absolute block inset-0'
  }
})
export class Table extends BaseTable<GoodsReceiptImportListItemDto, GetAllGoodsReceiptImportsRequest> {
  public readonly loading = input.required<boolean>();

  public readonly columns = GOODS_RECEIPT_IMPORT_TABLE_COLUMNS;

  constructor() {
    super();
  }

  public statusLabel(status: GoodsReceiptImportStatus): string {
    return GOODS_RECEIPT_IMPORT_STATUS_LABELS[status];
  }

  public statusColor(status: GoodsReceiptImportStatus): string {
    return GOODS_RECEIPT_IMPORT_STATUS_COLORS[status];
  }
}
