import { signalStore, withMethods, withState } from '@ngrx/signals';
// noinspection ES6PreferShortImport
import { withStockBatchCollection } from '../store-features/stock-batch-collection.feature';

type StockBatchState = {};
const initialState: StockBatchState = {};

export const StockBatchListStore = signalStore(
  withState(initialState),
  withStockBatchCollection(),
  withMethods((store) => {
    const reload = () => {
      store.load(store.filter());
    };

    return { reload };
  })
);
