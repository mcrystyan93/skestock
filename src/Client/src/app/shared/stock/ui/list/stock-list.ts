import {Component, effect, inject, input, untracked} from '@angular/core';
import {StockStore} from '../../services/stock.store';
import {TableContainer} from '../table/table-container';
import {FilterContainer} from './filter/filter-container';
import {isNil} from 'lodash-es';
import { ErrorAlert } from '@ske/shared/errors';

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
  public readonly locationId = input<string | null>(null);

  public readonly store = inject(StockStore);

  private readonly _loadEffectRef = effect(() => {
    const classId = this.classId();
    const locationId = this.locationId();

    if (isNil(classId))
      return;

    untracked(() => {
      this.store.load({ classId, locationId });
    });
  });

}
