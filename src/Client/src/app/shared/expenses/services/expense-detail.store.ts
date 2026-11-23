import { inject } from '@angular/core';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { mapResponse } from '@ngrx/operators';
import { patchState, signalStore, withMethods, withProps, withState } from '@ngrx/signals';
import { pipe, switchMap, tap } from 'rxjs';
import { ExpenseDto, ExpenseFieldsRequest } from '@ske/models';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { ExpensesHttp } from './expenses.http';

type ExpenseDetailState = {
  expense: ExpenseDto | null;
  saved: ExpenseDto | null;
  retried: boolean;
};

export type SaveExpenseRequest = {
  expenseId: string | null;
  classId: string;
  fields: ExpenseFieldsRequest;
};

export const ExpenseDetailStore = signalStore(
  withState<ExpenseDetailState>({ expense: null, saved: null, retried: false }),
  withLoadingFeature('expenseDetail'),
  withProblemDetailsFeature('expenseDetail'),
  withProps(() => ({
    expensesHttp: inject(ExpensesHttp)
  })),
  withMethods((store) => {
    const load = rxMethod<string>(
      pipe(
        tap(() => {
          store.clearExpenseDetailErrors();
          store.setExpenseDetailLoading();
        }),
        switchMap((id) =>
          store.expensesHttp.getById(id).pipe(
            mapResponse({
              next: (expense) => {
                patchState(store, { expense });
                store.setExpenseDetailLoaded();
              },
              error: (error) => {
                store.handleExpenseDetailError(error);
                store.setExpenseDetailLoaded();
              }
            })
          )
        )
      )
    );

    const save = rxMethod<SaveExpenseRequest>(
      pipe(
        tap(() => {
          store.clearExpenseDetailErrors();
          store.setExpenseDetailLoading();
        }),
        switchMap(({ expenseId, classId, fields }) =>
          (expenseId === null
              ? store.expensesHttp.create({ classId, fields })
              : store.expensesHttp.update(expenseId, fields)
          ).pipe(
            mapResponse({
              next: (saved) => {
                patchState(store, { saved });
                store.setExpenseDetailLoaded();
              },
              error: (error) => {
                store.handleExpenseDetailError(error);
                store.setExpenseDetailLoaded();
              }
            })
          )
        )
      )
    );

    const retry = rxMethod<string>(
      pipe(
        tap(() => {
          store.clearExpenseDetailErrors();
          store.setExpenseDetailLoading();
        }),
        switchMap((id) =>
          store.expensesHttp.retry(id).pipe(
            mapResponse({
              next: () => {
                patchState(store, { retried: true });
                store.setExpenseDetailLoaded();
              },
              error: (error) => {
                store.handleExpenseDetailError(error);
                store.setExpenseDetailLoaded();
              }
            })
          )
        )
      )
    );

    return { load, save, retry };
  })
);
