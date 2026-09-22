import { Component, effect, inject, input, untracked } from '@angular/core';
import { StockStore } from '../../services/stock.store';
import { StockListContainer } from '../table/stock-list-container';
import { FilterContainer } from './filter/filter-container';
import { isNil } from 'lodash-es';
import { ErrorAlert } from '@ske/shared/errors';
import { StockPreferencesService } from '../../services/stock-preferences.service';

@Component({
  imports: [
    StockListContainer,
    FilterContainer,
    ErrorAlert
  ],
  selector: 'ske-class-stock',
  styles: ``,
  templateUrl: './class-stock.html',
  providers: [StockStore],
  host: {
    class: 'flex flex-col grow gap-2 absolute inset-0'
  }
})
export class ClassStock {
  public readonly classId = input.required<string | null>();
  public readonly categoryId = input<string | null>(null);
  public readonly locationId = input<string | null>(null);

  public readonly store = inject(StockStore);
  private readonly _stockPreferences = inject(StockPreferencesService);

  private readonly _loadEffectRef = effect(() => {
    const classId = this.classId();
    const filters = untracked(() => this.store.filter().filters ?? []);
    const category = filters.find(x => x.field === 'categoryId') ?? null;
    const location = filters.find(x => x.field === 'locationId') ?? null;
    const includeHidden = untracked(() => this._stockPreferences.showHiddenProducts());

    if (isNil(classId))
      return;

    untracked(() => {
      const filters = [
        ...(category ? [category] : []),
        ...(location ? [location] : [])
      ];

      this.store.load({
        classId,
        filters,
        includeHidden,
        lowStockOnly: false,
        expiredOnly: false
      });
    });
  });

}
