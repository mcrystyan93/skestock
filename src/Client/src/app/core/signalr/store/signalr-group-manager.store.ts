import { patchState, signalStore, withMethods, withProps, withState } from '@ngrx/signals';
import { inject } from '@angular/core';
import { Dispatcher, Events, withEventHandlers } from '@ngrx/signals/events';
import { signalrEvents } from './signalr.events';
import { tap } from 'rxjs';

type GroupManagerState = {
  refCounts: Record<string, number>;
}

const initialState: GroupManagerState = { refCounts: {} };

export const SignalRGroupManagerStore = signalStore(
  { providedIn: 'root' },
  withState<GroupManagerState>(initialState),
  withProps(() => ({
    dispatcher: inject(Dispatcher),
    events: inject(Events)
  })),
  withMethods((store) => ({
    join(group: string): void {
      const count = store.refCounts()[group] ?? 0;
      patchState(store, (s) => ({ refCounts: { ...s.refCounts, [group]: count + 1 } }));
      if (count === 0) {
        store.dispatcher.dispatch(signalrEvents.invoke({ methodName: 'JoinGroup', args: [group] }));
      }
    },

    leave(group: string): void {
      const count = store.refCounts()[group] ?? 0;
      if (count <= 1) {
        patchState(store, (s) => {
          const { [group]: _, ...rest } = s.refCounts;
          return { refCounts: rest };
        });
        store.dispatcher.dispatch(signalrEvents.invoke({ methodName: 'LeaveGroup', args: [group] }));
      } else {
        patchState(store, (s) => ({ refCounts: { ...s.refCounts, [group]: count - 1 } }));
      }
    }
  })),
  // rejoin everything after a reconnect — new ConnectionId means group membership was lost server-side
  withEventHandlers((store) => ({
    onReconnected$: store.events.on(signalrEvents.reconnected).pipe(
      tap(() => {
        for (const group of Object.keys(store.refCounts())) {
          store.dispatcher.dispatch(signalrEvents.invoke({ methodName: 'JoinGroup', args: [group] }));
        }
      })
    )
  }))
);
