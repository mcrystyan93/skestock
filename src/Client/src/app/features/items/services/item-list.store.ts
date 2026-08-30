import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { withItemCollection } from '@ske/shared/items';
import { ItemDto } from '@ske/models';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap, tap } from 'rxjs';
import { mapResponse } from '@ngrx/operators';
import { inject } from '@angular/core';
import { ItemsHttp } from '@ske/shared/items';

type ItemListState = {
  togglingItemId: number | null;
};
const initialState: ItemListState = {
  togglingItemId: null
};

export const ItemListState = signalStore(
  withState(initialState),
  withItemCollection(),
  withMethods((store) => {
    const itemHttp = inject(ItemsHttp);

    const reload = () => {
      store.load(store.filter());
    };

    const toggleActive = rxMethod<ItemDto>(
      pipe(
        tap((item) => {
          store.clearItemsErrors();
          patchState(store, { togglingItemId: item.id });
        }),
        switchMap((item) => {
          const request$ = item.isActive ? itemHttp.disable(item.id) : itemHttp.enable(item.id);

          return request$.pipe(
            mapResponse({
              next: (updatedItem) => {
                patchState(store, {
                  items: store.items().map((i) => i.id === updatedItem.id ? updatedItem : i),
                  togglingItemId: null
                });
              },
              error: (error) => {
                store.handleItemsError(error);
                patchState(store, { togglingItemId: null });
              }
            })
          );
        })
      )
    );

    return { reload, toggleActive };
  })
);
