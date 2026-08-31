import { signalStore, withMethods, withState } from '@ngrx/signals';
import { withStockBatchCollection } from '@ske/shared/stock-batches';

type StockBatchState = {};
const initialState: StockBatchState = {};

export const StockBatchStore = signalStore(
  withState(initialState),
  withStockBatchCollection(),
  withMethods((store) => {
    const reload = () => {
      store.load(store.filter());
    };

    return { reload };
  })
);
