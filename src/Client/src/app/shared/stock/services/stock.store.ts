import { signalStore, withState } from '@ngrx/signals';
import { withStockCollection } from './stock-collection.feature';

type StockState = {};
const initialState: StockState = {};

export const StockStore = signalStore(
  withState(initialState),
  withStockCollection()
);
