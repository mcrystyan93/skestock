import { computed, inject } from '@angular/core';
import { mapResponse } from '@ngrx/operators';
import { patchState, signalStore, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { OrderCatalogItemDto, OrderListDto, SupplyListDto, SupplyListListItemDto } from '@ske/models';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { withLoadingFeature } from '@ske/shared/loader';
import { SupplyListsHttp } from '@ske/shared/supply-lists';
import { isNil } from 'lodash-es';
import { forkJoin, Observable, of, pipe, switchMap, tap } from 'rxjs';
import { catalogToLines } from './order-catalog';
import { OrderListsHttp } from './order-lists.http';
import {
  MAX_ORDER_LIST_NAME_LENGTH,
  MAX_ORDER_LIST_NOTE_LENGTH,
  supplyListsToLines,
  supplyListsToName,
  supplyListsToProducts
} from './order-list-wizard.utils';

export type OrderListSource = 'new' | 'supplyList';

export const ORDER_LIST_WIZARD_STEPS = ['source', 'selection', 'products', 'details', 'edit'] as const;
export type OrderListWizardStep = typeof ORDER_LIST_WIZARD_STEPS[number];

type OrderListWizardState = {
  step: OrderListWizardStep;
  source: OrderListSource | null;
  catalog: OrderCatalogItemDto[];
  catalogLoaded: boolean;
  selectedItemIds: string[];
  supplyLists: SupplyListListItemDto[];
  supplyListsLoaded: boolean;
  supplyListIds: string[];
  selectedSupplyLists: SupplyListDto[];
  /** Products the user unticked; kept as exclusions so a newly picked list starts fully ticked. */
  excludedItemIds: string[];
  name: string;
  note: string;
  /** The selection the name and note were last pre-filled from, so going back and forth keeps what was typed. */
  seededKey: string | null;
  loadedPrefillKey: string | null;
  loadFailed: boolean;
};

const initialState: OrderListWizardState = {
  step: 'source',
  source: null,
  catalog: [],
  catalogLoaded: false,
  selectedItemIds: [],
  supplyLists: [],
  supplyListsLoaded: false,
  supplyListIds: [],
  selectedSupplyLists: [],
  excludedItemIds: [],
  name: '',
  note: '',
  seededKey: null,
  loadedPrefillKey: null,
  loadFailed: false
};

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
    orderListsHttp: inject(OrderListsHttp),
    supplyListsHttp: inject(SupplyListsHttp)
  })),
  withComputed((store) => ({
    fromSupplyList: computed(() => store.source() === 'supplyList'),
    distinctSelectedItemCount: computed(() =>
      new Set(store.selectedSupplyLists().flatMap((list) => list.lines.map((line) => line.itemId))).size
    )
  })),
  withComputed((store) => ({
    // Only the supply list source reviews products; the catalog source already ticks its items in `selection`.
    steps: computed((): readonly OrderListWizardStep[] =>
      store.fromSupplyList() ? ORDER_LIST_WIZARD_STEPS : ORDER_LIST_WIZARD_STEPS.filter((step) => step !== 'products')
    ),
    products: computed(() => supplyListsToProducts(store.selectedSupplyLists())),
    /** Identifies the chosen supply lists alone, so ticking products never re-seeds the typed name. */
    listKey: computed(() => `supplyList:${[...store.supplyListIds()].sort().join(',')}`)
  })),
  withComputed((store) => ({
    stepIndex: computed(() => store.steps().indexOf(store.step())),
    selectedProductIds: computed(() => {
      const excluded = new Set(store.excludedItemIds());

      return store.products().map((product) => product.itemId).filter((id) => !excluded.has(id));
    })
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
        case 'products':
          return store.selectedProductIds().length > 0;
        case 'details':
          return store.name().trim().length > 0
            && store.name().length <= MAX_ORDER_LIST_NAME_LENGTH
            && store.note().length <= MAX_ORDER_LIST_NOTE_LENGTH;
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
        ? `${store.listKey()}|${[...store.excludedItemIds()].sort().join(',')}`
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
      (classId: string) => store.orderListsHttp.getCatalog(classId),
      (catalog) => patchState(store, { catalog, catalogLoaded: true, selectedItemIds: [] })
    );

    const loadSupplyLists = loadInto(
      slice(store.setSupplyListsLoading, store.clearSupplyListsErrors, store.setSupplyListsLoaded, store.handleSupplyListsError),
      (_: void) => store.supplyListsHttp.getAllActive(),
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
      setDetails({ name, note }: { name: string; note: string }) {
        patchState(store, { name, note });
      },
      setSelectedProductIds(selectedIds: string[]) {
        const selected = new Set(selectedIds);

        patchState(store, {
          excludedItemIds: store.products().map((product) => product.itemId).filter((id) => !selected.has(id))
        });
      },
      selectSupplyLists(supplyListIds: string[]) {
        patchState(store, { supplyListIds, excludedItemIds: [] });
        loadSelectedSupplyLists(supplyListIds);
      },
      /** Moves to the step after the current one and loads the data it needs. Returns whether it moved. */
      advance(classId: string | null): boolean {
        if (!store.canGoNext())
          return false;

        if (store.step() === 'source') {
          if (store.source() === 'new' && !store.catalogLoaded() && !isNil(classId))
            loadSuggestions(classId);

          if (store.fromSupplyList() && !store.supplyListsLoaded())
            loadSupplyLists();
        }

        if (store.step() === 'selection' && store.seededKey() !== (store.fromSupplyList() ? store.listKey() : store.prefillKey())) {
          const lists = store.selectedSupplyLists();

          patchState(store, store.fromSupplyList()
            ? {
              name: supplyListsToName(lists) ?? '',
              note: lists.length === 1 ? lists[0].note ?? '' : '',
              seededKey: store.listKey()
            }
            : { seededKey: store.prefillKey() });
        }

        patchState(store, { step: store.steps()[store.stepIndex() + 1] });

        return true;
      },
      back() {
        patchState(store, { step: store.steps()[Math.max(0, store.stepIndex() - 1)] });
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
        const lines = store.fromSupplyList()
          ? supplyListsToLines(store.selectedSupplyLists()).filter((line) => !store.excludedItemIds().includes(line.itemId ?? ''))
          : catalogToLines(store.catalog(), store.selectedItemIds());

        return { name: store.name().trim(), note: store.note(), lines };
      }
    };
  })
);
