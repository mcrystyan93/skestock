// noinspection ES6PreferShortImport

import { patchState, signalStore, withState } from '@ngrx/signals';
import { withStockCollection } from './stock-collection.feature';
import { withQueryParamsSync } from '@ske/routes';

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
  withQueryParamsSync({
    key: 'categoryId',
    getValue: (store) => () => store.categoryIdQueryParam(),
    setValue: (store, value) => patchState(store, { categoryIdQueryParam: value }),
    parse: (raw) => raw ?? null,
    serialize: (value) => value ?? ''
  }),
  withQueryParamsSync({
    key: 'locationId',
    getValue: (store) => () => store.locationIdQueryParam(),
    setValue: (store, value) => patchState(store, { locationIdQueryParam: value }),
    parse: (raw) => raw ?? null,
    serialize: (value) => value ?? ''
  })
);
