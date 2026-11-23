import { inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { Events, withEventHandlers } from '@ngrx/signals/events';
import { mapResponse } from '@ngrx/operators';
import { EMPTY, filter, map, pipe, switchMap, tap } from 'rxjs';
import {
  buildExpenseListFilter,
  ExpenseDto,
  GetAllExpensesRequest,
  PAGINATION_PAGE_SIZE,
  PaginatedResponseData
} from '@ske/models';
import { realtimeEvents } from '@ske/signalr';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { ExpensesHttp } from './expenses.http';

type ExpenseListState = {
  expenses: ExpenseDto[];
  paginationData: PaginatedResponseData | null;
  filter: GetAllExpensesRequest;
  isLoadingMore: boolean;
};

const initialState: ExpenseListState = {
  expenses: [],
  paginationData: null,
  filter: {
    sort: [],
    filters: [],
    cursor: null,
    pageSize: PAGINATION_PAGE_SIZE,
    searchTerm: null
  },
  isLoadingMore: false
};

export const ExpenseListStore = signalStore(
  withState(initialState),
  withLoadingFeature('expenses'),
  withProblemDetailsFeature('expenses'),
  withComputed((store) => ({
    hasNextPage: () => store.paginationData()?.hasNextPage ?? false,
    nextCursor: () => store.paginationData()?.nextCursor ?? null
  })),
  withProps(() => ({
    expensesHttp: inject(ExpensesHttp)
  })),
  withMethods((store) => {
    const load = rxMethod<Partial<GetAllExpensesRequest>>(
      pipe(
        map((data) => buildExpenseListFilter(store.filter(), data)),
        tap((filter) => {
          store.clearExpensesErrors();
          store.setExpensesLoading();
          patchState(store, { filter, isLoadingMore: false });
        }),
        switchMap((filter) =>
          store.expensesHttp.getAll(filter).pipe(
            mapResponse({
              next: (result) => {
                patchState(store, {
                  expenses: result.data,
                  paginationData: result,
                  filter: { ...filter, cursor: null, sort: result.sort }
                });
                store.setExpensesLoaded();
              },
              error: (error) => {
                store.handleExpensesError(error);
                store.setExpensesLoaded();
              }
            })
          )
        )
      )
    );

    const loadMore = rxMethod<void>(
      pipe(
        filter(() => store.hasNextPage() && !store.isLoadingMore()),
        tap(() => {
          store.clearExpensesErrors();
          patchState(store, { isLoadingMore: true });
        }),
        switchMap(() => {
          const filter = store.filter();
          const nextCursor = store.nextCursor();

          if (!nextCursor) {
            patchState(store, { isLoadingMore: false });
            return EMPTY;
          }

          return store.expensesHttp.getAll({ ...filter, cursor: nextCursor }).pipe(
            mapResponse({
              next: (result) => {
                patchState(store, {
                  expenses: [...store.expenses(), ...result.data],
                  paginationData: result,
                  filter: { ...filter, cursor: null, sort: result.sort },
                  isLoadingMore: false
                });
              },
              error: (error) => {
                store.handleExpensesError(error);
                patchState(store, { isLoadingMore: false });
              }
            })
          );
        })
      )
    );

    const reload = () => load(store.filter());

    const remove = rxMethod<string>(
      pipe(
        switchMap((id) =>
          store.expensesHttp.delete(id).pipe(
            mapResponse({
              next: () => reload(),
              error: (error) => store.handleExpensesError(error)
            })
          )
        )
      )
    );

    const retry = rxMethod<string>(
      pipe(
        switchMap((id) =>
          store.expensesHttp.retry(id).pipe(
            mapResponse({
              next: () => reload(),
              error: (error) => store.handleExpensesError(error)
            })
          )
        )
      )
    );

    return { load, loadMore, reload, remove, retry };
  }),
  // The list is scoped to one class and the page only joins that class's group, so any expense event is relevant.
  withEventHandlers((store, events = inject(Events)) => ({
    expenseChanged: events
      .on(realtimeEvents.expenseCreated, realtimeEvents.expenseProcessed, realtimeEvents.expenseFailed)
      .pipe(tap(() => store.reload()))
  }))
);
