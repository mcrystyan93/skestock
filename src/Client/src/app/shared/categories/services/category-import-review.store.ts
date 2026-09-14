import { inject } from '@angular/core';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { mapResponse } from '@ngrx/operators';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { EMPTY, pipe, switchMap, takeWhile, tap, timer } from 'rxjs';
import {
  CategoryImportBatchFileDto,
  CategoryImportBatchReviewDto,
  CategoryImportReviewLineDto,
  CategoryDropdownValue,
  ConfirmCategoryImportBatchResponse,
  ImportBatchHistoryDto
} from '@ske/models';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { CategoryImportsHttp } from './category-imports.http';

type CategoryImportReviewState = {
  importId: string | null;
  review: CategoryImportBatchReviewDto | null;
  lines: CategoryImportReviewEditableLine[];
  files: CategoryImportBatchFileDto[];
  history: ImportBatchHistoryDto[];
  confirmation: ConfirmCategoryImportBatchResponse | null;
};

const initialState: CategoryImportReviewState = {
  importId: null,
  review: null,
  lines: [],
  files: [],
  history: [],
  confirmation: null
};

const BATCH_REVIEW_POLL_INTERVAL_MS = 2000;

export type CategoryImportReviewEditableLine = CategoryImportReviewLineDto & {
  rowId: string;
  category: CategoryDropdownValue;
};

export const CategoryImportReviewState = signalStore(
  withState(initialState),
  withLoadingFeature('review'),
  withLoadingFeature('reviewConfirm'),
  withProblemDetailsFeature('review'),
  withMethods((store) => {
    const http = inject(CategoryImportsHttp);

    const load = rxMethod<string>(
      pipe(
        tap((importId) => {
          store.clearReviewErrors();
          store.setReviewLoading();
          patchState(store, {
            importId,
            review: null,
            lines: [],
            files: [],
            history: [],
            confirmation: null
          });
        }),
        switchMap((importId) =>
          timer(0, BATCH_REVIEW_POLL_INTERVAL_MS).pipe(
            switchMap(() => http.getBatchById(importId)),
            takeWhile((review) => review.status === 'processing', true),
            mapResponse({
              next: (review) => {
                patchState(store, {
                  review,
                  lines: review.suggestions.map((suggestion) => ({
                    ...suggestion,
                    category: suggestion.matchedCategory ?? null,
                    rowId: crypto.randomUUID()
                  })),
                  files: review.files,
                  history: review.history
                });

                if (review.status !== 'processing')
                  store.setReviewLoaded();
              },
              error: (error) => {
                store.handleReviewError(error);
                store.setReviewLoaded();
              }
            })
          )
        )
      )
    );

    const confirm = rxMethod<CategoryImportReviewEditableLine[]>(
      pipe(
        tap(() => {
          store.clearReviewErrors();
          store.setReviewConfirmLoading();
        }),
        switchMap((lines) => {
          const importId = store.importId();

          if (!importId) {
            store.setReviewConfirmLoaded();
            return EMPTY;
          }

          return http.confirmBatch(importId, {
            names: lines
              .map((line) => line.category?.name?.trim() ?? line.name.trim())
              .filter(Boolean)
          }).pipe(
            mapResponse({
              next: (confirmation) => {
                patchState(store, { confirmation });
                store.setReviewConfirmLoaded();
              },
              error: (error) => {
                store.handleReviewError(error);
                store.setReviewConfirmLoaded();
              }
            })
          );
        })
      )
    );

    return { load, confirm };
  })
);
