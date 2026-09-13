import { inject } from '@angular/core';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { mapResponse } from '@ngrx/operators';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { EMPTY, pipe, switchMap, tap } from 'rxjs';
import {
  CategoryDropdownValue,
  ConfirmItemImportBatchRequest,
  ConfirmItemImportBatchResponse,
  ImportBatchFileDto,
  ImportBatchHistoryDto,
  ItemDropdownValue,
  ItemImportBatchReviewDto,
  ItemImportReviewLineDto
} from '@ske/models';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { ItemImportsHttp } from './item-imports.http';

export type ItemImportReviewEditableLine = Omit<ItemImportReviewLineDto, 'matchedCategory' | 'matchedItem'> & {
  rowId: string;
  item: ItemDropdownValue | null;
  category: CategoryDropdownValue | null;
};

type ItemImportReviewState = {
  importId: string | null;
  review: ItemImportBatchReviewDto | null;
  lines: ItemImportReviewEditableLine[];
  files: ImportBatchFileDto[];
  history: ImportBatchHistoryDto[];
  confirmation: ConfirmItemImportBatchResponse | null;
};

const initialState: ItemImportReviewState = {
  importId: null,
  review: null,
  lines: [],
  files: [],
  history: [],
  confirmation: null
};

const nextRowId = () => crypto.randomUUID();
const BATCH_REVIEW_POLL_INTERVAL_MS = 2000;

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
          http.getBatchById(importId).pipe(mapResponse({
              next: (review) => {
                patchState(store, {
                  review,
                  lines: review.suggestions.map((line) => ({
                    ...line,
                    item: line.matchedItem ?? null,
                    category: line.matchedCategory ?? null,
                    rowId: nextRowId()
                  })),
                  files: review.files,
                  history: review.history
                });
                
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

          const request: ConfirmItemImportBatchRequest = {
            items: lines.map((line) => ({
              sku: line.sku,
              itemId: line.item?.id ?? '',
              name: line.name,
              unit: line.unit,
              description: line.description,
              isPerishable: line.isPerishable
            }))
          };

          return http.confirmBatch(importId, request).pipe(
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
