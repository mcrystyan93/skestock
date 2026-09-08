import { Component, effect, inject, input, untracked } from '@angular/core';
import { Table } from '@ske/shared/goods-receipts';
import { SchoolClassOverviewStore } from '../../../services/school-class-overview.store';
import { isNil } from 'lodash-es';
import { ColumnFilter } from '@ske/models';

@Component({
  imports: [
    Table
  ],
  selector: 'ske-school-class-overview-goods-receipts-tab',
  styles: ``,
  template: `

    <ske-goods-receipts-table [items]="store.goodsReceipts()"
                              [filter]="store.filter()"
                              [loading]="store.goodsReceiptsLoading()"
                              [hasNextPage]="store.hasGoodsReceiptsNextPage()"
                              [isLoadingMore]="store.isLoadingMoreGoodsReceipts()"
                              (onFilterChange)="store.loadGoodsReceipts($event)"
                              (onLoadMore)="store.loadMoreGoodsReceipts()" />

  `
})
export class GoodsReceiptsTab {
  public readonly classId = input.required<string | null>();
  public readonly store = inject(SchoolClassOverviewStore);

  private readonly _classIdEffectRef = effect(() => {
    const classIdValue = this.classId();

    if (isNil(classIdValue))
      return;

    untracked(() => {
      this.store.loadGoodsReceipts({ ...this.store.filter(), ...{ filters: [this.getClassIdFilter(classIdValue)] } });
    });
  });

  private getClassIdFilter(classId: string): ColumnFilter {
    return {
      value: classId,
      operator: 'equals',
      fieldType: 'number',
      field: 'classId'
    };
  }
}
