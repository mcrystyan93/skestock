import { Component, effect, inject, input, untracked } from '@angular/core';
import { Table } from './table';
import { StockBatchStore } from '../../services/stock-batch.store';
import { isNil } from 'lodash-es';
import { ColumnFilter } from '@ske/models';

@Component({
  imports: [
    Table
  ],
  selector: 'ske-stock-batches-table-container',
  styles: ``,
  templateUrl: './table-container.html',
  host: {
    class: 'absolute block inset-0'
  },
  providers: [StockBatchStore]
})
export class TableContainer {
  // public readonly items = input.required<Array<StockBatchListItemDto>>();
  // public readonly filter = input.required<GetAllStockBatchesRequest>();
  // public readonly loading = input.required<boolean>();
  // public readonly isReady = input(false);
  // public readonly hasNextPage = input.required<boolean>();
  // public readonly isLoadingMore = input.required<boolean>();
  //
  // public readonly onFilterChange = output<GetAllStockBatchesRequest>();
  // public readonly onLoadMore = output<void>();
  public readonly goodsReceiptId = input.required<number>();

  public readonly store = inject(StockBatchStore);

  private readonly _goodsReceiptIdEffectRef = effect(() => {
    const goodsReceiptId = this.goodsReceiptId();

    if (isNil(goodsReceiptId))
      return;

    untracked(() => {
      this.store.load({ ...this.store.filter(), ...{ filters: [this.getClassIdFilter(goodsReceiptId)] } });
    });
  });

  private getClassIdFilter(goodsReceiptId: number): ColumnFilter {
    return {
      value: goodsReceiptId,
      operator: 'equals',
      fieldType: 'number',
      field: 'goodsReceiptId'
    };
  }
}
