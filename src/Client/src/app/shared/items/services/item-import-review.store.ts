import { inject } from '@angular/core';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { mapResponse } from '@ngrx/operators';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { EMPTY, pipe, switchMap, tap } from 'rxjs';
import {
  ConfirmItemImportRequest,
  ItemImportConfirmationResultDto,
  ItemImportReviewDto,
  ItemImportReviewLineDto
} from '@ske/models';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { ItemImportsHttp } from './item-imports.http';

export type ItemImportReviewEditableLine = ItemImportReviewLineDto & { rowId: string };

type ItemImportReviewState = {
  importId: string | null;
  review: ItemImportReviewDto | null;
  lines: ItemImportReviewEditableLine[];
  confirmation: ItemImportConfirmationResultDto | null;
};

const initialState: ItemImportReviewState = {
  importId: null,
  review: null,
  lines: [],
  confirmation: null
};

const nextRowId = () => crypto.randomUUID();

export const ItemImportReviewState = signalStore(
  withState(initialState),
  withLoadingFeature('review'),
  withLoadingFeature('reviewConfirm'),
  withProblemDetailsFeature('review'),
  withMethods((store) => {
    const http = inject(ItemImportsHttp);

    const load = rxMethod<string>(
      pipe(
        tap((importId) => {
          store.clearReviewErrors();
          store.setReviewLoading();
          patchState(store, { importId, confirmation: null });
        }),
        switchMap((importId) => http.getById(importId).pipe(
          mapResponse({
            next: (review) => {
              patchState(store, {
                review,
                lines: review.suggestions.map((line) => ({ ...line, rowId: nextRowId() }))
              });
              store.setReviewLoaded();
            },
            error: (error) => {
              store.handleReviewError(error);
              store.setReviewLoaded();
            }
          })
        ))
      )
    );

    const confirm = rxMethod<ItemImportReviewEditableLine[]>(
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

          const request: ConfirmItemImportRequest = {
            items: lines.map(({ rowId: _, ...line }) => line)
          };

          return http.confirm(importId, request).pipe(
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
