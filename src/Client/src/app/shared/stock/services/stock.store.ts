import { signalStore, withState } from '@ngrx/signals';
import { withStockCollection } from '../store-features/stock-collection.feature';

type StockState = {};
const initialState: StockState = {};

export const StockStore = signalStore(
  withState(initialState),
  withStockCollection()
);
