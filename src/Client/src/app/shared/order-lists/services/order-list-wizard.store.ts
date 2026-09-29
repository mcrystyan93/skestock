import { computed, inject } from '@angular/core';
import { mapResponse } from '@ngrx/operators';
import { patchState, signalStore, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { OrderListDto, PAGINATION_PAGE_SIZE, SupplyListDto, SupplyListListItemDto } from '@ske/models';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { withLoadingFeature } from '@ske/shared/loader';
import { StockHttp } from '@ske/shared/stock';
import { SupplyListsHttp } from '@ske/shared/supply-lists';
import { isNil } from 'lodash-es';
import { EMPTY, expand, forkJoin, Observable, of, pipe, reduce, switchMap, tap } from 'rxjs';
import {
  groupLowStockItems,
  SuggestedItem,
  suggestionsToLines,
  supplyListsToLines,
  supplyListsToName
} from './order-list-wizard.utils';

export type OrderListSource = 'new' | 'supplyList';

export const ORDER_LIST_WIZARD_STEPS = ['source', 'selection', 'edit'] as const;
export type OrderListWizardStep = typeof ORDER_LIST_WIZARD_STEPS[number];

type OrderListWizardState = {
  step: OrderListWizardStep;
  source: OrderListSource | null;
  suggestions: SuggestedItem[];
  suggestionsLoaded: boolean;
  selectedItemIds: string[];
  supplyLists: SupplyListListItemDto[];
  supplyListsLoaded: boolean;
  supplyListIds: string[];
  selectedSupplyLists: SupplyListDto[];
  loadedPrefillKey: string | null;
  loadFailed: boolean;
};

const initialState: OrderListWizardState = {
  step: 'source',
  source: null,
  suggestions: [],
  suggestionsLoaded: false,
  selectedItemIds: [],
  supplyLists: [],
  supplyListsLoaded: false,
  supplyListIds: [],
  selectedSupplyLists: [],
  loadedPrefillKey: null,
  loadFailed: false
};

// The API caps the page size, so the whole active set is read by following the cursor.
function fetchAllActiveSupplyLists(http: SupplyListsHttp) {
  const page = (cursor: string | null) => http.getAll({
    pageSize: PAGINATION_PAGE_SIZE,
    cursor,
    filters: [{ field: 'isActive', operator: 'equals', value: true, fieldType: 'boolean' }],
    sort: [{ key: 'name', value: 'ascend' }]
  });

  return page(null).pipe(
    expand((response) => response.hasNextPage && response.nextCursor ? page(response.nextCursor) : EMPTY),
    reduce((all, response) => [...all, ...response.data], [] as SupplyListListItemDto[])
  );
}

type LoadSlice = {
  start: () => void;
  done: () => void;
  fail: (error: unknown) => void;
};

/** Runs `request` for every input, latest wins, keeping the slice's loading and error state in sync. */
function loadInto<TInput, TResult>(
  slice: LoadSlice,
  request: (input: TInput) => Observable<TResult>,
  onNext: (result: TResult) => void
) {
  return rxMethod<TInput>(
    pipe(
      tap(() => slice.start()),
      switchMap((input) =>
        request(input).pipe(
          mapResponse({
            next: (result) => {
              onNext(result);
              slice.done();
            },
            error: (error) => {
              slice.fail(error);
              slice.done();
            }
          })
        )
      )
    )
  );
}

export const OrderListWizardStore = signalStore(
  withState(initialState),
  withLoadingFeature('suggestions'),
  withProblemDetailsFeature('suggestions'),
  withLoadingFeature('supplyLists'),
  withProblemDetailsFeature('supplyLists'),
  withLoadingFeature('selectedSupplyLists'),
  withProblemDetailsFeature('selectedSupplyLists'),
  withProps(() => ({
    stockHttp: inject(StockHttp),
    supplyListsHttp: inject(SupplyListsHttp)
  })),
  withComputed((store) => ({
    stepIndex: computed(() => ORDER_LIST_WIZARD_STEPS.indexOf(store.step())),
    fromSupplyList: computed(() => store.source() === 'supplyList'),
    distinctSelectedItemCount: computed(() =>
      new Set(store.selectedSupplyLists().flatMap((list) => list.lines.map((line) => line.itemId))).size
    )
  })),
  withComputed((store) => ({
    canGoNext: computed(() => {
      switch (store.step()) {
        case 'source':
          return store.source() !== null;
        case 'selection':
          if (store.fromSupplyList())
            return store.supplyListIds().length > 0
              && store.selectedSupplyLists().length === store.supplyListIds().length
              && !store.selectedSupplyListsLoading();

          return !store.suggestionsLoading() && !store.loadFailed();
        case 'edit':
          return false;
      }
    }),
    activeProblemDetail: computed(() =>
      store.fromSupplyList()
        ? store.supplyListsProblemDetail() ?? store.selectedSupplyListsProblemDetail()
        : store.suggestionsProblemDetail()
    ),
    activeValidationErrors: computed(() =>
      store.fromSupplyList() ? store.supplyListsValidationErrors() : store.suggestionsValidationErrors()
    ),
    /** Identifies what the editor would be pre-filled with, so unchanged choices never rebuild it. */
    prefillKey: computed(() =>
      store.fromSupplyList()
        ? `supplyList:${[...store.supplyListIds()].sort().join(',')}`
        : `new:${[...store.selectedItemIds()].sort().join(',')}`
    )
  })),
  withMethods((store) => {
    // Not every failure carries a problem detail (e.g. a dropped connection), so failure is tracked here too.
    const slice = (
      setLoading: () => void,
      clearErrors: () => void,
      setLoaded: () => void,
      handleError: (error: unknown) => void
    ): LoadSlice => ({
      start: () => {
        setLoading();
        clearErrors();
        patchState(store, { loadFailed: false });
      },
      done: setLoaded,
      fail: (error) => {
        handleError(error);
        patchState(store, { loadFailed: true });
      }
    });

    const loadSuggestions = loadInto(
      slice(store.setSuggestionsLoading, store.clearSuggestionsErrors, store.setSuggestionsLoaded, store.handleSuggestionsError),
      (classId: string) => store.stockHttp.getLowStockItems(classId),
      (items) => {
        const suggestions = groupLowStockItems(items);

        patchState(store, {
          suggestions,
          suggestionsLoaded: true,
          selectedItemIds: suggestions.map((item) => item.itemId)
        });
      }
    );

    const loadSupplyLists = loadInto(
      slice(store.setSupplyListsLoading, store.clearSupplyListsErrors, store.setSupplyListsLoaded, store.handleSupplyListsError),
      (_: void) => fetchAllActiveSupplyLists(store.supplyListsHttp),
      (supplyLists) => patchState(store, { supplyLists, supplyListsLoaded: true })
    );

    const loadSelectedSupplyLists = loadInto(
      slice(
        store.setSelectedSupplyListsLoading,
        store.clearSelectedSupplyListsErrors,
        store.setSelectedSupplyListsLoaded,
        store.handleSelectedSupplyListsError
      ),
      (ids: string[]) => ids.length === 0
        ? of([] as SupplyListDto[])
        : forkJoin(ids.map((id) => store.supplyListsHttp.getById(id))),
      (selectedSupplyLists) => patchState(store, { selectedSupplyLists })
    );

    return {
      setSource(source: OrderListSource) {
        patchState(store, { source });
      },
      setSelectedItemIds(selectedItemIds: string[]) {
        patchState(store, { selectedItemIds });
      },
      selectSupplyLists(supplyListIds: string[]) {
        patchState(store, { supplyListIds });
        loadSelectedSupplyLists(supplyListIds);
      },
      /** Moves to the step after the current one and loads the data it needs. Returns whether it moved. */
      advance(classId: string | null): boolean {
        if (!store.canGoNext())
          return false;

        if (store.step() === 'source') {
          if (store.source() === 'new' && !store.suggestionsLoaded() && !isNil(classId))
            loadSuggestions(classId);

          if (store.fromSupplyList() && !store.supplyListsLoaded())
            loadSupplyLists();
        }

        patchState(store, { step: ORDER_LIST_WIZARD_STEPS[store.stepIndex() + 1] });

        return true;
      },
      back() {
        patchState(store, { step: ORDER_LIST_WIZARD_STEPS[Math.max(0, store.stepIndex() - 1)] });
      },
      /** Reloads whatever the current step needs, including the details of the chosen supply lists. */
      retry(classId: string | null) {
        if (!store.fromSupplyList()) {
          if (!isNil(classId))
            loadSuggestions(classId);

          return;
        }

        if (!store.supplyListsLoaded())
          loadSupplyLists();
        else
          loadSelectedSupplyLists(store.supplyListIds());
      },
      /** Remembers what the editor was pre-filled with. */
      markPrefillLoaded() {
        patchState(store, { loadedPrefillKey: store.prefillKey() });
      },
      /** The order-list fields the editor is pre-filled with when the last step opens. */
      buildPrefill(): Partial<OrderListDto> {
        if (store.fromSupplyList()) {
          const lists = store.selectedSupplyLists();

          return {
            name: supplyListsToName(lists),
            note: lists.length === 1 ? lists[0].note ?? null : null,
            lines: supplyListsToLines(lists)
          };
        }

        return { lines: suggestionsToLines(store.suggestions(), store.selectedItemIds()) };
      }
    };
  })
);
