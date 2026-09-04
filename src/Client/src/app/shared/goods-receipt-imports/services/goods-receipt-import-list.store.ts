import { signalStore, withMethods, withState } from '@ngrx/signals';
import { withGoodsReceiptImportCollection } from '@ske/shared/goods-receipt-imports';

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
