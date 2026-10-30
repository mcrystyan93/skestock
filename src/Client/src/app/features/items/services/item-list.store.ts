import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { Events, withEventHandlers } from '@ngrx/signals/events';
import { withItemCollection } from '@ske/shared/items';
import { ItemDto } from '@ske/models';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { mergeMap, pipe, switchMap, tap } from 'rxjs';
import { mapResponse } from '@ngrx/operators';
import { inject } from '@angular/core';
import { ItemsHttp } from '@ske/shared/items';
import { realtimeEvents } from '@ske/signalr';

type ItemListState = {
  togglingItemId: string | null;
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

    // The edit endpoint replaces the whole item, so the row's current fields are sent back unchanged.
    // mergeMap, not switchMap: saves for different rows must not cancel each other.
    const setItemImage = rxMethod<{ item: ItemDto; fileId: string }>(
      pipe(
        tap(() => store.clearItemsErrors()),
        mergeMap(({ item, fileId }) =>
          itemHttp.edit(item.id, {
            sku: item.sku,
            name: item.name,
            description: item.description,
            unit: item.unit,
            minThreshold: item.minThreshold,
            isPerishable: item.isPerishable,
            shelfLifeDays: item.shelfLifeDays,
            categoryId: item.categoryId,
            imageFileId: fileId
          }).pipe(
            mapResponse({
              next: (updatedItem) => {
                patchState(store, {
                  items: store.items().map((i) => i.id === updatedItem.id ? updatedItem : i)
                });
              },
              error: (error) => {
                store.handleItemsError(error);
              }
            })
          )
        )
      )
    );

    return { reload, toggleActive, setItemImage };
  }),
  withEventHandlers((store, events = inject(Events)) => ({
    itemChanges: events.on(
      realtimeEvents.itemCreated,
      realtimeEvents.itemUpdated,
      realtimeEvents.itemDisabled,
      realtimeEvents.itemEnabled,
      realtimeEvents.itemImportBatchConfirmed
    ).pipe(tap(() => store.reload()))
  }))
);
