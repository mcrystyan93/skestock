import { inject } from '@angular/core';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { mapResponse } from '@ngrx/operators';
import { patchState, signalStore, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import {
  buildCategoryImportListFilter,
  CategoryImportBatchDto,
  CategoryImportDto,
  CategoryImportListItemDto,
  CreateCategoryImportBatchRequest,
  CreateCategoryImportRequest,
  GetAllCategoryImportsRequest,
  PAGINATION_PAGE_SIZE,
  PaginatedResponseData
} from '@ske/models';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { CategoryImportsHttp } from './category-imports.http';
import { EMPTY, concatMap, filter, from, map, pipe, switchMap, tap, toArray } from 'rxjs';
import { Events, withEventHandlers } from '@ngrx/signals/events';
import { realtimeEvents } from '@ske/signalr';

type CategoryImportStateModel = {
  categoryImports: CategoryImportDto[];
  categoryImportBatch: CategoryImportBatchDto | null;
  categoryImportListItems: CategoryImportListItemDto[];
  categoryImportListPaginationData: PaginatedResponseData | null;
  categoryImportListFilter: GetAllCategoryImportsRequest;
  categoryImportListLoadingMore: boolean;
};

const initialState: CategoryImportStateModel = {
  categoryImports: [],
  categoryImportBatch: null,
  categoryImportListItems: [],
  categoryImportListPaginationData: null,
  categoryImportListFilter: {
    sort: [],
    filters: [],
    cursor: null,
    pageSize: PAGINATION_PAGE_SIZE,
    searchTerm: null
  },
  categoryImportListLoadingMore: false
};

export const CategoryImportState = signalStore(
  withState(initialState),
  withLoadingFeature('categoryImport'),
  withLoadingFeature('categoryImportList'),
  withProblemDetailsFeature('categoryImport'),
  withProblemDetailsFeature('categoryImportList'),
  withProps(() => ({
    categoryImportsHttp: inject(CategoryImportsHttp)
  })),
  withComputed((store) => ({
    categoryImportListHasNextPage: () => store.categoryImportListPaginationData()?.hasNextPage ?? false,
    categoryImportListNextCursor: () => store.categoryImportListPaginationData()?.nextCursor ?? null
  })),
  withMethods((store) => {
    const createImports = rxMethod<CreateCategoryImportRequest[]>(
      pipe(
        tap(() => {
          store.clearCategoryImportErrors();
          store.setCategoryImportLoading();
          patchState(store, { categoryImports: [], categoryImportBatch: null });
        }),
        concatMap((requests) => from(requests).pipe(
          concatMap((request) => store.categoryImportsHttp.create(request)),
          toArray()
        )),
        mapResponse({
          next: (categoryImports) => {
            patchState(store, { categoryImports });
            store.setCategoryImportLoaded();
          },
          error: (error) => {
            store.handleCategoryImportError(error);
            store.setCategoryImportLoaded();
          }
        })
      )
    );

    const createBatch = rxMethod<CreateCategoryImportBatchRequest>(
      pipe(
        tap(() => {
          store.clearCategoryImportErrors();
          store.setCategoryImportLoading();
          patchState(store, { categoryImports: [], categoryImportBatch: null });
        }),
        concatMap((request) => store.categoryImportsHttp.createBatch(request)),
        mapResponse({
          next: (categoryImportBatch) => {
            patchState(store, { categoryImportBatch });
            store.setCategoryImportLoaded();
          },
          error: (error) => {
            store.handleCategoryImportError(error);
            store.setCategoryImportLoaded();
          }
        })
      )
    );

    const load = rxMethod<GetAllCategoryImportsRequest>(
      pipe(
        map((data) => buildCategoryImportListFilter(store.categoryImportListFilter(), data)),
        tap((filter) => {
          store.clearCategoryImportListErrors();
          store.setCategoryImportListLoading();
          patchState(store, { categoryImportListFilter: filter, categoryImportListLoadingMore: false });
        }),
        switchMap((filter) =>
          store.categoryImportsHttp.getAll(filter)
            .pipe(
              mapResponse({
                next: (result) => {
                  patchState(store, {
                    categoryImportListItems: result.data,
                    categoryImportListPaginationData: result,
                    categoryImportListFilter: { ...filter, cursor: null, sort: result.sort }
                  });
                  store.setCategoryImportListLoaded();
                },
                error: (error) => {
                  store.handleCategoryImportListError(error);
                  store.setCategoryImportListLoaded();
                }
              })
            )
        )
      )
    );

    const loadMore = rxMethod<void>(
      pipe(
        filter(() => store.categoryImportListHasNextPage() && !store.categoryImportListLoadingMore()),
        tap(() => {
          store.clearCategoryImportListErrors();
          patchState(store, { categoryImportListLoadingMore: true });
        }),
        switchMap(() => {
          const currentFilter = store.categoryImportListFilter();
          const nextCursor = store.categoryImportListNextCursor();

          if (!nextCursor) {
            patchState(store, { categoryImportListLoadingMore: false });
            return EMPTY;
          }

          return store.categoryImportsHttp.getAll({ ...currentFilter, cursor: nextCursor })
            .pipe(
              mapResponse({
                next: (result) => {
                  patchState(store, {
                    categoryImportListItems: [...store.categoryImportListItems(), ...result.data],
                    categoryImportListPaginationData: result,
                    categoryImportListFilter: { ...currentFilter, cursor: null, sort: result.sort },
                    categoryImportListLoadingMore: false
                  });
                  store.setCategoryImportListLoaded();
                },
                error: (error) => {
                  store.handleCategoryImportListError(error);
                  patchState(store, { categoryImportListLoadingMore: false });
                }
              })
            );
        })
      )
    );

    const reload = () => load(store.categoryImportListFilter());

    return { createImports, createBatch, load, loadMore, reload };
  }),
  withEventHandlers((store, events = inject(Events)) => ({
    categoryImportChanges: events.on(
      realtimeEvents.categoryImportCreated,
      realtimeEvents.categoryImportProcessed,
      realtimeEvents.categoryImportConfirmed
    ).pipe(
      tap(() => store.reload())
    )
  }))
);
