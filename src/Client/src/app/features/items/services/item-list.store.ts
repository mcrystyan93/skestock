import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { Events, withEventHandlers } from '@ngrx/signals/events';
import { withItemCollection } from '@ske/shared/items';
import { ItemDto, MergeItemsRequest } from '@ske/models';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { NzMessageService } from 'ng-zorro-antd/message';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { filter, groupBy, mergeMap, pipe, switchMap, tap } from 'rxjs';
import { mapResponse } from '@ngrx/operators';
import { inject } from '@angular/core';
import { ItemsHttp } from '@ske/shared/items';
import { realtimeEvents } from '@ske/signalr';

type ItemListState = {
  togglingItemId: string | null;
  /** Snapshot of the selected rows, so they survive filter changes that unload them. */
  selected: Record<string, ItemDto>;
  merging: boolean;
};
const initialState: ItemListState = {
  togglingItemId: null,
  selected: {},
  merging: false
};

export const ItemListState = signalStore(
  withState(initialState),
  withItemCollection(),
  withProblemDetailsFeature('merge'),
  withComputed((store) => ({
    selectedItems: () => Object.values(store.selected()),
    selectedIds: () => new Set(Object.keys(store.selected())),
    selectedCount: () => Object.keys(store.selected()).length
  })),
  withMethods((store) => {
    const itemHttp = inject(ItemsHttp);
    const message = inject(NzMessageService);

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
            locationThresholds: [],
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

    // Realtime row events replace just that row, so the list keeps its loaded pages and scroll position.
    // A row that was edited out of its sort position or filter is corrected on the next load.
    const patchItem = rxMethod<string>(
      pipe(
        filter((id) => store.items().some((i) => i.id === id)),
        // Per row, a newer event cancels the older fetch so a late response cannot overwrite fresher data;
        // rows do not cancel each other.
        groupBy((id) => id),
        mergeMap((row$) =>
          row$.pipe(
            switchMap((id) =>
              itemHttp.getById(id).pipe(
                mapResponse({
                  next: (updatedItem) => {
                    patchState(store, {
                      items: store.items().map((i) => i.id === updatedItem.id ? updatedItem : i)
                    });
                  },
                  error: () => reload()
                })
              )
            )
          )
        )
      )
    );

    const toggleSelected = (item: ItemDto, checked: boolean) => {
      const { [item.id]: _, ...rest } = store.selected();

      patchState(store, { selected: checked ? { ...rest, [item.id]: item } : rest });
    };

    // Applies to the loaded rows only; rows selected earlier and no longer loaded stay as they are.
    const setLoadedSelected = (checked: boolean) => {
      const selected = { ...store.selected() };

      for (const item of store.items()) {
        if (checked)
          selected[item.id] = item;
        else
          delete selected[item.id];
      }

      patchState(store, { selected });
    };

    const clearSelection = () => patchState(store, { selected: {} });

    // switchMap: a second merge while one runs would double-submit the same ids.
    const merge = rxMethod<{ request: MergeItemsRequest; onSuccess?: () => void }>(
      pipe(
        tap(() => {
          store.clearMergeErrors();
          patchState(store, { merging: true });
        }),
        switchMap(({ request, onSuccess }) =>
          itemHttp.merge(request).pipe(
            mapResponse({
              next: () => {
                patchState(store, { merging: false, selected: {} });
                reload();
                message.success('Itemele au fost unite.');
                onSuccess?.();
              },
              error: (error) => {
                store.handleMergeError(error);
                patchState(store, { merging: false });
                message.error('Itemele nu au putut fi unite.');
              }
            })
          )
        )
      )
    );

    return { reload, toggleActive, setItemImage, patchItem, toggleSelected, setLoadedSelected, clearSelection, merge };
  }),
  withEventHandlers((store, events = inject(Events)) => ({
    itemListChanges: events.on(
      realtimeEvents.itemCreated,
      realtimeEvents.itemImportBatchConfirmed
    ).pipe(tap(() => store.reload())),
    itemRowChanges: events.on(
      realtimeEvents.itemUpdated,
      realtimeEvents.itemDisabled,
      realtimeEvents.itemEnabled
    ).pipe(tap(({ payload }) => store.patchItem(payload.itemId)))
  }))
);
