import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Component, effect, input, output, signal, untracked } from '@angular/core';
import { GetAllGoodsReceiptsRequest, GoodsReceiptListItemDto } from '@ske/models';
import { BaseList } from '@ske/shared/tables';
import { TimeAgoPipe } from '@ske/shared/pipes';
import { TableContainer as StockBatchesTableContainer } from '@ske/shared/stock-batches';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { NzEmptyComponent } from 'ng-zorro-antd/empty';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzSkeletonComponent } from 'ng-zorro-antd/skeleton';
import { NzSpinComponent } from 'ng-zorro-antd/spin';
import { NzTooltipDirective } from 'ng-zorro-antd/tooltip';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { StopClick } from '@ske/shared/directives';

@Component({
  imports: [
    CurrencyPipe,
    DatePipe,
    DecimalPipe,
    NzButtonComponent,
    NzCardComponent,
    NzEmptyComponent,
    NzIconDirective,
    NzSkeletonComponent,
    NzSpinComponent,
    NzTooltipDirective,
    NzTypographyComponent,
    StockBatchesTableContainer,
    StopClick,
    TimeAgoPipe
  ],
  selector: 'ske-goods-receipt-list-small',
  templateUrl: './goods-receipt-list-small.html',
  host: {
    class: 'absolute inset-0 flex flex-col overflow-hidden'
  }
})
export class GoodsReceiptListSmall extends BaseList<GoodsReceiptListItemDto, GetAllGoodsReceiptsRequest> {
  public readonly loading = input.required<boolean>();
  public readonly expandedReceiptId = input<string | null>(null);
  public readonly downloadFile = output<GoodsReceiptListItemDto>();
  public readonly expandedRows = signal<Set<string>>(new Set());

  protected readonly skeletonPlaceholders = [0, 1, 2, 3];

  private readonly _expandedReceiptEffect = effect(() => {
    const receiptId = this.expandedReceiptId();
    if (!receiptId || !this.items().some(item => item.id === receiptId)) {
      return;
    }

    untracked(() => this.expandedRows.set(new Set([receiptId])));
  });

  public isExpanded(id: string): boolean {
    return this.expandedRows().has(id);
  }

  public toggleRow(id: string): void {
    this.expandedRows.update(previous => {
      const next = new Set(previous);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  }
}
