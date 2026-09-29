import { inject } from '@angular/core';
import { signalStore, withMethods, withState } from '@ngrx/signals';
import { Events, withEventHandlers } from '@ngrx/signals/events';
import { realtimeEvents } from '@ske/signalr';
import { tap } from 'rxjs';
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
  }),
  withEventHandlers((store, events = inject(Events)) => ({
    orderListChanges: events.on(
      realtimeEvents.orderListCreated,
      realtimeEvents.orderListSubmitted,
      realtimeEvents.orderListCancelled,
      realtimeEvents.orderListReopened
    ).pipe(tap(() => store.reload()))
  }))
);
