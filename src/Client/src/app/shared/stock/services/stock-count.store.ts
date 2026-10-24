import { computed, inject } from '@angular/core';
import { mapResponse } from '@ngrx/operators';
import { patchState, signalStore, type, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { eventGroup, injectDispatch } from '@ngrx/signals/events';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { map, pipe, switchMap, tap } from 'rxjs';
import { AdjustmentReason, ErrorCodes, StockItemDto } from '@ske/models';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { withLoadingFeature } from '@ske/shared/loader';
import { StockHttp } from './stock.http';

export type StockCountStep = 'locations' | 'items' | 'summary';

export type StockCountChange = {
  key: string;
  row: StockItemDto;
  newQuantity: number;
  delta: number;
};

type StockCountState = {
  classId: string | null;
  rows: StockItemDto[];
  selectedLocationIds: string[];
  reason: AdjustmentReason;
  /** Edited values only, keyed by `rowKey`. A value equal to the current quantity is never stored. */
  counts: Record<string, number>;
  step: StockCountStep;
  currentIndex: number;
  completed: boolean;
};

const initialState: StockCountState = {
  classId: null,
  rows: [],
  selectedLocationIds: [],
  reason: AdjustmentReason.Adjustment,
  counts: {},
  step: 'locations',
  currentIndex: 0,
  completed: false
};

export const stockCountApiEvents = eventGroup({
  source: 'Stock API',
  events: {
    countSuccess: type<void>()
  }
});

export const stockCountRowKey = (row: Pick<StockItemDto, 'itemId' | 'locationId'>) => `${row.itemId}:${row.locationId}`;

const compare = (a: string, b: string) => a.localeCompare(b, 'ro');

/** Guided physical recount of a class's stock, saved atomically via `POST /api/Stock/class/{id}/bulk-adjust`. */
export const StockCountState = signalStore(
  withState(initialState),
  withLoadingFeature('stockCount'),
  withProblemDetailsFeature('stockCount'),
  withProps(() => ({
    stockHttp: inject(StockHttp),
    dispatcher: injectDispatch(stockCountApiEvents)
  })),
  withComputed((store) => {
    const locations = computed(() => {
      const byId = new Map<string, { name: string; itemCount: number }>();
      store.rows().forEach((row) => {
        const entry = byId.get(row.locationId) ?? { name: row.locationName, itemCount: 0 };
        byId.set(row.locationId, { ...entry, itemCount: entry.itemCount + 1 });
      });
      return [...byId]
        .map(([id, { name, itemCount }]) => ({ id, name, itemCount }))
        .sort((a, b) => compare(a.name, b.name));
    });

    const rowsInScope = computed(() => {
      const selected = new Set(store.selectedLocationIds());
      return store
        .rows()
        .filter((row) => selected.has(row.locationId))
        .sort(
          (a, b) =>
            compare(a.locationName, b.locationName) ||
            compare(a.categoryName, b.categoryName) ||
            compare(a.itemName, b.itemName)
        );
    });

    const changes = computed<StockCountChange[]>(() =>
      rowsInScope().flatMap((row) => {
        const key = stockCountRowKey(row);
        const newQuantity = store.counts()[key];
        return newQuantity === undefined ? [] : [{ key, row, newQuantity, delta: newQuantity - row.quantity }];
      })
    );

    return {
      locations,
      rowsInScope,
      changes,
      currentRow: computed(() => rowsInScope()[store.currentIndex()] ?? null),
      unchangedCount: computed(() => rowsInScope().length - changes().length)
    };
  }),
  withMethods((store) => {
    const fetchRows = (classId: string) =>
      store.stockHttp
        .getClassLocationStock({
          classId,
          filters: [],
          searchTerm: null,
          includeHidden: false,
          lowStockOnly: false,
          expiredOnly: false
        })
        .pipe(map((report) => report.items.filter((item) => item.quantity > 0)));

    const load = rxMethod<string>(
      pipe(
        tap((classId) => {
          patchState(store, { classId });
          store.setStockCountLoading();
          store.clearStockCountErrors();
        }),
        switchMap((classId) =>
          fetchRows(classId).pipe(
            mapResponse({
              next: (rows) => {
                patchState(store, { rows });
                store.setStockCountLoaded();
              },
              error: (error) => {
                store.handleStockCountError(error);
                store.setStockCountLoaded();
              }
            })
          )
        )
      )
    );

    // Keeps the error shown and the user's counts, but refreshes rows so the next submit sends current expectedQuantity.
    const reloadAfterConflict = rxMethod<void>(
      pipe(
        switchMap(() =>
          fetchRows(store.classId()!).pipe(
            mapResponse({
              next: (rows) => {
                const quantities = new Map(rows.map((row) => [stockCountRowKey(row), row.quantity]));
                patchState(store, (state) => ({
                  rows,
                  counts: Object.fromEntries(
                    Object.entries(state.counts).filter(([key, value]) => {
                      const quantity = quantities.get(key);
                      return quantity !== undefined && quantity !== value;
                    })
                  )
                }));
                patchState(store, {
                  currentIndex: Math.min(store.currentIndex(), Math.max(store.rowsInScope().length - 1, 0))
                });
                store.setStockCountLoaded();
              },
              error: () => store.setStockCountLoaded()
            })
          )
        )
      )
    );

    const submit = rxMethod<void>(
      pipe(
        tap(() => {
          store.setStockCountLoading();
          store.clearStockCountErrors();
        }),
        switchMap(() =>
          store.stockHttp
            .bulkAdjustStock(store.classId()!, {
              reason: store.reason(),
              lines: store.changes().map(({ row, newQuantity }) => ({
                itemId: row.itemId,
                locationId: row.locationId,
                actualQuantity: newQuantity,
                expectedQuantity: row.quantity
              }))
            })
            .pipe(
              mapResponse({
                next: () => {
                  patchState(store, { completed: true });
                  store.dispatcher.countSuccess();
                  store.setStockCountLoaded();
                },
                error: (error) => {
                  store.handleStockCountError(error);
                  if (store.stockCountProblemDetail()?.error?.code === ErrorCodes.stock.bulkConcurrencyConflict)
                    reloadAfterConflict();
                  else store.setStockCountLoaded();
                }
              })
            )
        )
      )
    );

    return {
      load,
      submit,
      setReason: (reason: AdjustmentReason) => patchState(store, { reason }),
      setSelectedLocationIds: (selectedLocationIds: string[]) => patchState(store, { selectedLocationIds }),
      setCount: (row: StockItemDto, value: number) =>
        patchState(store, (state) => {
          const { [stockCountRowKey(row)]: _, ...rest } = state.counts;
          return {
            counts: value === row.quantity ? rest : { ...rest, [stockCountRowKey(row)]: value }
          };
        }),
      next: () => {
        if (store.step() === 'locations') {
          if (store.rowsInScope().length > 0) patchState(store, { step: 'items', currentIndex: 0 });
        } else if (store.step() === 'items') {
          if (store.currentIndex() < store.rowsInScope().length - 1) {
            patchState(store, { currentIndex: store.currentIndex() + 1 });
          } else {
            patchState(store, { step: 'summary' });
          }
        }
      },
      previous: () => {
        if (store.step() === 'summary') {
          patchState(store, {
            step: 'items',
            currentIndex: Math.max(store.rowsInScope().length - 1, 0)
          });
        } else if (store.step() === 'items') {
          if (store.currentIndex() > 0) patchState(store, { currentIndex: store.currentIndex() - 1 });
          else patchState(store, { step: 'locations' });
        }
      },
      jumpToSummary: () => patchState(store, { step: 'summary' }),
      goToRow: (key: string) => {
        const index = store.rowsInScope().findIndex((row) => stockCountRowKey(row) === key);
        if (index >= 0) patchState(store, { step: 'items', currentIndex: index });
      }
    };
  })
);
