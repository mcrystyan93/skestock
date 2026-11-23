import { inject } from '@angular/core';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { mapResponse } from '@ngrx/operators';
import { patchState, signalStore, withMethods, withProps, withState } from '@ngrx/signals';
import { concatMap, from, pipe, tap, toArray } from 'rxjs';
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
};

// One uploaded file becomes one expense (created as "În procesare" and queued for AI extraction).
export const ExpenseUploadStore = signalStore(
  withState<ExpenseUploadState>({ expenses: [] }),
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
          patchState(store, { expenses: [] });
        }),
        concatMap(({ files, classId }) =>
          from(files).pipe(
            concatMap((file) => store.expensesHttp.createFromDocument({ classId, fileMetadataId: file.id })),
            toArray()
          )
        ),
        mapResponse({
          next: (expenses) => {
            patchState(store, { expenses });
            store.setExpenseUploadLoaded();
          },
          error: (error) => {
            store.handleExpenseUploadError(error);
            store.setExpenseUploadLoaded();
          }
        })
      )
    );

    return { createFromDocuments };
  })
);
