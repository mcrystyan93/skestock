import { inject } from '@angular/core';
import { signalStore, withMethods } from '@ngrx/signals';
import { Events, withEventHandlers } from '@ngrx/signals/events';
import { withCategoryCollection } from '@ske/shared/categories';
import { realtimeEvents } from '@ske/signalr';
import { tap } from 'rxjs';

export const CategoryListState = signalStore(
  withCategoryCollection(),
  withMethods((store) => {
    const reload = () => {
      store.load(store.filter());
    };

    return { reload };
  }),
  withEventHandlers((store, events = inject(Events)) => ({
    categoryImportBatchConfirmed: events.on(realtimeEvents.categoryImportBatchConfirmed).pipe(
      tap(() => store.reload())
    )
  }))
);
