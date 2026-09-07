import { signalStore, withMethods, withState } from '@ngrx/signals';
// noinspection ES6PreferShortImport
import { withGoodsReceiptImportCollection } from '../store-features/goods-receipt-import-collection.feature';

type GoodsReceiptImportState = {};
const initialState: GoodsReceiptImportState = {};

export const GoodsReceiptImportListStore = signalStore(
  withState(initialState),
  withGoodsReceiptImportCollection(),
  withMethods((store) => {
    const reload = () => {
      store.load(store.filter());
    };

    return { reload };
  })
);
