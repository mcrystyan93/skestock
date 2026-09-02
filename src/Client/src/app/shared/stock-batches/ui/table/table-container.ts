import { Component, effect, inject, input, untracked } from '@angular/core';
import { Table } from './table';
import { StockBatchListStore } from '../../services/stock-batch-list.store';
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
  providers: [StockBatchListStore]
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
  public readonly goodsReceiptId = input.required<string>();

  public readonly store = inject(StockBatchListStore);

  private readonly _goodsReceiptIdEffectRef = effect(() => {
    const goodsReceiptId = this.goodsReceiptId();

    if (isNil(goodsReceiptId))
      return;

    untracked(() => {
      this.store.load({ ...this.store.filter(), ...{ filters: [this.getClassIdFilter(goodsReceiptId)] } });
    });
  });

  private getClassIdFilter(goodsReceiptId: string): ColumnFilter {
    return {
      value: goodsReceiptId,
      operator: 'equals',
      fieldType: 'number',
      field: 'goodsReceiptId'
    };
  }
}
