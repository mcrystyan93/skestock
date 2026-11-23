import { inject } from '@angular/core';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { patchState, signalStore, withMethods, withProps, withState } from '@ngrx/signals';
import { catchError, concatMap, from, map, of, pipe, tap, toArray } from 'rxjs';
import { ExpenseDto, FileMetadataDto } from '@ske/models';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { ExpensesHttp } from './expenses.http';

export type ExpenseUploadFiles = {
  files: FileMetadataDto[];
  classId: string;
};

type ExpenseUploadState = {
  expenses: ExpenseDto[];
  /** Files whose expense could not be created while others succeeded. */
  failedCount: number;
};

// One uploaded file becomes one expense (created as "În procesare" and queued for AI extraction).
export const ExpenseUploadStore = signalStore(
  withState<ExpenseUploadState>({ expenses: [], failedCount: 0 }),
  withLoadingFeature('expenseUpload'),
  withProblemDetailsFeature('expenseUpload'),
  withProps(() => ({
    expensesHttp: inject(ExpensesHttp)
  })),
  withMethods((store) => {
    const createFromDocuments = rxMethod<ExpenseUploadFiles>(
      pipe(
        tap(() => {
          store.clearExpenseUploadErrors();
          store.setExpenseUploadLoading();
          patchState(store, { expenses: [], failedCount: 0 });
        }),
        concatMap(({ files, classId }) =>
          from(files).pipe(
            // A failing file must not discard the expenses already created: a retry would duplicate them.
            concatMap((file) => store.expensesHttp.createFromDocument({ classId, fileMetadataId: file.id }).pipe(
              map((expense) => ({ expense, error: null })),
              catchError((error: unknown) => of({ expense: null, error }))
            )),
            toArray()
          )
        ),
        tap((results) => {
          const expenses = results.flatMap((result) => (result.expense ? [result.expense] : []));
          const firstError = results.find((result) => result.error)?.error;

          if (expenses.length === 0 && firstError) {
            store.handleExpenseUploadError(firstError);
          } else {
            patchState(store, { expenses, failedCount: results.length - expenses.length });
          }

          store.setExpenseUploadLoaded();
        })
      )
    );

    return { createFromDocuments };
  })
);
