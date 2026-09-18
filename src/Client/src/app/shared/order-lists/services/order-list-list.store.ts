import { signalStore, withMethods, withState } from '@ngrx/signals';
import { withOrderListCollection } from './order-list-collection.feature';

type OrderListListState = {};
const initialState: OrderListListState = {};

export const OrderListListStore = signalStore(
  withState(initialState),
  withOrderListCollection(),
  withMethods((store) => {
    const reload = () => {
      store.load(store.filter());
    };

    return { reload };
  })
);
