import { Component, effect, inject, input, untracked } from '@angular/core';
import { StockStore } from '../../services/stock.store';
import { TableContainer } from '../table/table-container';
import { FilterContainer } from './filter/filter-container';
import { isNil } from 'lodash-es';
import { ErrorAlert } from '@ske/shared/errors';
import { ColumnFilter } from '@ske/models';

@Component({
  imports: [
    TableContainer,
    FilterContainer,
    ErrorAlert
  ],
  selector: 'ske-stock-list',
  styles: ``,
  templateUrl: './stock-list.html',
  providers: [StockStore],
  host: {
    class: 'flex flex-col grow absolute inset-0'
  }
})
export class StockList {
  public readonly classId = input.required<string | null>();
  public readonly categoryId = input<string | null>(null);
  public readonly locationId = input<string | null>(null);

  public readonly store = inject(StockStore);

  private readonly _loadEffectRef = effect(() => {
    const classId = this.classId();
    const categoryId = this.store.categoryIdQueryParam();
    const locationId = this.store.locationIdQueryParam();

    if (isNil(classId))
      return;

    untracked(() => {
      const filters = [
        ...(categoryId ? [this.getCategoryIdFilter(categoryId)] : []),
        ...(locationId ? [this.getLocationIdFilter(locationId)] : [])
      ];

      this.store.load({ classId, filters });
    });
  });

  private getCategoryIdFilter(categoryId: string): ColumnFilter {
    return {
      value: categoryId,
      operator: 'equals',
      fieldType: 'number',
      field: 'categoryId'
    };
  }

  private getLocationIdFilter(locationId: string): ColumnFilter {
    return {
      value: locationId,
      operator: 'equals',
      fieldType: 'number',
      field: 'locationId'
    };
  }

}
