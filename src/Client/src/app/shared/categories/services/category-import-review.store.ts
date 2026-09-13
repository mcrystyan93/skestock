import { inject } from '@angular/core';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { mapResponse } from '@ngrx/operators';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { EMPTY, pipe, switchMap, takeWhile, tap, timer } from 'rxjs';
import {
  CategoryImportBatchFileDto,
  CategoryImportBatchReviewDto,
  CategoryImportReviewDto,
  ConfirmCategoryImportBatchResponse,
  ConfirmCategoryImportResponse,
  ImportBatchHistoryDto
} from '@ske/models';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { CategoryImportsHttp } from './category-imports.http';

export type CategoryImportReviewTarget = {
  importId: string;
  isBatch?: boolean;
};

type CategoryImportReviewState = {
  importId: string | null;
  isBatch: boolean;
  review: CategoryImportReviewDto | CategoryImportBatchReviewDto | null;
  names: string[];
  files: CategoryImportBatchFileDto[];
  history: ImportBatchHistoryDto[];
  confirmation: ConfirmCategoryImportResponse | ConfirmCategoryImportBatchResponse | null;
};

const initialState: CategoryImportReviewState = {
  importId: null,
  isBatch: false,
  review: null,
  names: [],
  files: [],
  history: [],
  confirmation: null
};

const BATCH_REVIEW_POLL_INTERVAL_MS = 2000;

export const CategoryImportReviewState = signalStore(
  withState(initialState),
  withLoadingFeature('review'),
  withLoadingFeature('reviewConfirm'),
  withProblemDetailsFeature('review'),
  withMethods((store) => {
    const http = inject(CategoryImportsHttp);

    const load = rxMethod<CategoryImportReviewTarget>(
      pipe(
        tap(({ importId, isBatch = false }) => {
          store.clearReviewErrors();
          store.setReviewLoading();
          patchState(store, {
            importId,
            isBatch,
            review: null,
            names: [],
            files: [],
            history: [],
            confirmation: null
          });
        }),
        switchMap(({ importId, isBatch = false }) => {
          const request = isBatch
            ? timer(0, BATCH_REVIEW_POLL_INTERVAL_MS).pipe(
              switchMap(() => http.getBatchById(importId)),
              takeWhile((review) => review.status === 'processing', true)
            )
            : http.getById(importId);

          return request.pipe(
            mapResponse({
              next: (review) => {
                const batchReview = isBatch ? review as CategoryImportBatchReviewDto : null;

                patchState(store, {
                  review,
                  names: review.suggestions.map((suggestion) => suggestion.name),
                  files: batchReview?.files ?? [],
                  history: batchReview?.history ?? []
                });
                if (!isBatch || review.status !== 'processing')
                  store.setReviewLoaded();
              },
              error: (error) => {
                store.handleReviewError(error);
                store.setReviewLoaded();
              }
            })
          );
        })
      )
    );

    const updateName = (index: number, event: Event) => {
      const target = event.target;

      if (!(target instanceof HTMLInputElement))
        return;

      const names = [...store.names()];
      if (index < 0 || index >= names.length)
        return;

      names[index] = target.value;
      patchState(store, { names });
    };

    const removeName = (index: number) => {
      patchState(store, {
        names: store.names().filter((_, currentIndex) => currentIndex !== index)
      });
    };

    const confirm = rxMethod<void>(
      pipe(
        tap(() => {
          store.clearReviewErrors();
          store.setReviewConfirmLoading();
        }),
        switchMap(() => {
          const importId = store.importId();

          if (!importId) {
            store.setReviewConfirmLoaded();
            return EMPTY;
          }

          const request = {
            names: store.names().map((name) => name.trim()).filter(Boolean)
          };
          const handleConfirmation = (
            confirmation: ConfirmCategoryImportResponse | ConfirmCategoryImportBatchResponse
          ) => {
            patchState(store, { confirmation });
            store.setReviewConfirmLoaded();
          };
          const handleError = (error: unknown) => {
            store.handleReviewError(error);
            store.setReviewConfirmLoaded();
          };

          if (store.isBatch()) {
            return http.confirmBatch(importId, request).pipe(
              mapResponse({
                next: handleConfirmation,
                error: handleError
              })
            );
          }

          return http.confirm(importId, request).pipe(
            mapResponse({
              next: handleConfirmation,
              error: handleError
            })
          );
        })
      )
    );

    return { load, updateName, removeName, confirm };
  })
);
