import { inject } from '@angular/core';
import { signalStore, withMethods } from '@ngrx/signals';
import { Events, withEventHandlers } from '@ngrx/signals/events';
import { realtimeEvents } from '@ske/signalr';
import { withSupplyListCollection } from '@ske/shared/supply-lists';
import { tap } from 'rxjs';

export const SupplyListListState = signalStore(
  withSupplyListCollection(),
  withMethods((store) => ({
    reload: () => store.load(store.filter())
  })),
  withEventHandlers((store, events = inject(Events)) => ({
    supplyListChanges: events.on(
      realtimeEvents.supplyListCreated,
      realtimeEvents.supplyListUpdated,
      realtimeEvents.supplyListDisabled,
      realtimeEvents.supplyListEnabled
    ).pipe(tap(() => store.reload()))
  }))
);
