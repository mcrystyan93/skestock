import { Component, effect, inject, input, untracked } from '@angular/core';
import { GoodsReceiptImportListStore } from '../../services/goods-receipt-import-list.store';
import { FilterContainer } from './filter/filter-container';
import { TableContainer } from '../table/table-container';
import { ColumnFilter } from '@ske/models';
import { isNil } from 'lodash-es';
import { ErrorAlert } from '@ske/shared/errors';
import { NzTabComponent } from 'ng-zorro-antd/tabs';

@Component({
  imports: [
    FilterContainer,
    TableContainer,
    ErrorAlert,
    NzTabComponent
  ],
  selector: 'ske-goods-receipt-imports-list',
  styles: ``,
  templateUrl: './goods-receipt-imports-list.html',
  providers: [GoodsReceiptImportListStore],
  host: {
    class: 'flex flex-col grow absolute inset-0'
  }
})
export class GoodsReceiptImportsList {
  public readonly classId = input.required<string | null>();

  public readonly store = inject(GoodsReceiptImportListStore);

  private readonly _loadEffectRef = effect(() => {
    const classId = this.classId();

    if (isNil(classId))
      return;

    untracked(() => {
      this.store.load({ ...this.store.filter(), filters: [this.getClassIdFilter(classId)] });
    });
  });

  private getClassIdFilter(classId: string): ColumnFilter {
    return {
      value: classId,
      operator: 'equals',
      fieldType: 'string',
      field: 'classId'
    };
  }
}
