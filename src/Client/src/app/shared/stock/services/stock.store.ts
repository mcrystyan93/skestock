// noinspection ES6PreferShortImport

import { patchState, signalStore, withComputed, withState } from '@ngrx/signals';
import { withStockCollection } from './stock-collection.feature';
import { withQueryParamsSync } from '@ske/routes';
import { ColumnFilter } from '@ske/models';

type StockState = {
  categoryIdQueryParam: string | null;
  locationIdQueryParam: string | null;
};
const initialState: StockState = {
  categoryIdQueryParam: null,
  locationIdQueryParam: null
};

export const StockStore = signalStore(
  withState(initialState),
  withStockCollection(),
  withComputed((store) => ({
    categoryId: () => (store.filter().filters ?? []).find(x => x.field === 'categoryId')?.value ?? null,
    locationId: () => (store.filter().filters ?? []).find(x => x.field === 'locationId')?.value ?? null
  })),
  withQueryParamsSync({
    key: 'categoryId',
    getValue: (store) => () => store.categoryId(),
    setValue: (store, value) => {
      // Update the filter in the state with the new categoryId value
      const currentFilters: ColumnFilter[] = store.filter().filters ?? [];
      const updatedFilters = currentFilters.filter(x => x.field !== 'categoryId') as ColumnFilter[];
      if (value) {
        updatedFilters.push({ field: 'categoryId', value, fieldType: 'string', operator: 'equals' });
      }
      patchState(store, { filter: { ...store.filter(), filters: updatedFilters } });
    },
    parse: (raw) => raw ?? null,
    serialize: (value) => value ?? ''
  }),
  withQueryParamsSync({
    key: 'locationId',
    getValue: (store) => () => store.locationId(),
    setValue: (store, value) => {
      // Update the filter in the state with the new locationId value
      const currentFilters: ColumnFilter[] = store.filter().filters ?? [];
      const updatedFilters = currentFilters.filter(x => x.field !== 'locationId') as ColumnFilter[];
      if (value) {
        updatedFilters.push({ field: 'locationId', value, fieldType: 'string', operator: 'equals' });
      }
      patchState(store, { filter: { ...store.filter(), filters: updatedFilters } });
    },
    parse: (raw) => raw ?? null,
    serialize: (value) => value ?? ''
  })
);
