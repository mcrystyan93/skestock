import { inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { mapResponse } from '@ngrx/operators';
import { EMPTY, pipe, switchMap, tap } from 'rxjs';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { GoodsReceiptImportsHttp } from './goods-receipt-imports.http';
import {
  ConfirmGoodsReceiptImportLineRequest,
  ConfirmGoodsReceiptImportRequest,
  GoodsReceiptDto,
  GoodsReceiptImportReviewDto,
  GoodsReceiptImportReviewLineDto,
  ItemDropdownValue,
  LocationDropdownValue,
  toDateOnlyString
} from '@ske/models';

/**
 * Client-only editable row built from one AI-extracted line (`GoodsReceiptImportReviewLineDto`).
 * Splitting a source line produces several rows sharing the same `sourceLineIndex`/`originalQuantity`
 * - the server (and `groupTotals` below) requires their quantities to sum back to the original.
 */
export type ReviewEditableLine = {
  rowId: string;
  sourceLineIndex: number;
  originalQuantity: number;
  rawItemText?: string | null;
  extractedName?: string | null;
  extractedCategory?: string | null;
  extractedUnit?: string | null;
  extractedSku?: string | null;
  item: ItemDropdownValue;
  location: LocationDropdownValue;
  quantity: number;
  expiryDate: Date | null;
  unitPrice: number;
};

/**
 * Payload emitted by the review lines table for a structural mutation (split/remove). It carries
 * the table's current *edited* working copy so the store applies the change on top of unsaved edits
 * rather than its own stale `state.lines`.
 */
export type LineMutationEvent = {
  rowId: string;
};

/** A line is perishable iff the selected item is - there is no independent editable flag. */
export function isLinePerishable(line: ReviewEditableLine): boolean {
  return line.item?.isPerishable ?? false;
}

type ReviewState = {
  importId: string | null;
  review: GoodsReceiptImportReviewDto | null;
  lines: ReviewEditableLine[];
  supplierReference: string | null;
  note: string;
  confirmedReceipt: GoodsReceiptDto | null;
};

const initialState: ReviewState = {
  importId: null,
  review: null,
  lines: [],
  supplierReference: null,
  note: '',
  confirmedReceipt: null
};

/**
 * `rowId` must stay globally unique for the lifetime of the page: it's the `@for` track key and the
 * signal-forms array identity. A module-level counter is fragile (dev-server HMR reloads or the
 * module being duplicated across bundler chunks can reset it back to the same starting value,
 * producing collisions), so generate ids with `crypto.randomUUID()` instead.
 */
const nextRowId = () => `${crypto.randomUUID()}`;

/** A split divides a row's quantity into this many parts (halves the source, adds the remainder). */
const SPLIT_DIVISOR = 2;

function buildEditableLine(line: GoodsReceiptImportReviewLineDto, sourceLineIndex: number): ReviewEditableLine {
  const matchedItem = line.matchedItem;

  return {
    rowId: nextRowId(),
    sourceLineIndex,
    originalQuantity: line.quantity,
    rawItemText: line.rawItemText,
    extractedName: line.name,
    extractedCategory: line.category,
    extractedUnit: line.unit,
    extractedSku: line.productCode,
    item: matchedItem
      ? {
        id: matchedItem.id,
        name: matchedItem.name,
        sku: matchedItem.sku,
        unit: matchedItem.unit,
        isPerishable: matchedItem.isPerishable,
        categoryId: matchedItem.categoryId,
        categoryName: matchedItem.categoryName
      }
      : null,
    location: null,
    quantity: line.quantity,
    expiryDate: null,
    unitPrice: line.unitPrice ?? 0
  };
}

/**
 * Store for the goods-receipt-import review modal. Loads the AI extraction (SKU-matched against
 * the item catalog) and builds an editable working copy of its lines, then posts the reviewed/
 * corrected lines to the confirm endpoint, which creates the real `GoodsReceipt`.
 */
export const ReviewStore = signalStore(
  withState(initialState),
  withLoadingFeature('review'),
  withLoadingFeature('reviewConfirm'),
  withProblemDetailsFeature('review'),
  withMethods((store) => {
    const http = inject(GoodsReceiptImportsHttp);

    const load = rxMethod<string>(
      pipe(
        tap((importId) => {
          store.clearReviewErrors();
          store.setReviewLoading();

          patchState(store, { importId, confirmedReceipt: null });
        }),
        switchMap((importId) =>
          http.getById(importId).pipe(
            mapResponse({
              next: (review) => {
                patchState(store, {
                  review,
                  lines: review.lines.map((line, index) => buildEditableLine(line, index)),
                  supplierReference: review.supplierReference ?? null,
                  note: ''
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

    /**
     * Split a row into two rows of the same source line: the source keeps the larger half, a new
     * row (reset location/expiry) takes the remainder. Applied on the emitted edited snapshot so
     * unsaved edits on other rows are preserved when the table's linkedSignal recomputes.
     */
    const splitLine = ({ rowId }: LineMutationEvent) => {
      const storeLines = store.lines();
      const index = storeLines.findIndex((line) => line.rowId === rowId);

      if (index === -1)
        return;

      const source = storeLines[index];
      const half = Math.floor(source.quantity / SPLIT_DIVISOR);
      const updatedSource: ReviewEditableLine = { ...source, quantity: source.quantity - half };
      const newRow: ReviewEditableLine = {
        ...source,
        rowId: nextRowId(),
        quantity: half,
        location: null,
        expiryDate: null
      };

      const next = [...storeLines];
      next.splice(index, 1, updatedSource, newRow);

      patchState(store, { lines: next });
    };

    /** Remove a split row - never the last remaining row of a source-line group. */
    const removeLine = ({ rowId }: LineMutationEvent) => {
      const storeLines = store.lines();
      const line = storeLines.find((l) => l.rowId === rowId);

      if (!line)
        return;

      const groupSize = storeLines.filter((l) => l.sourceLineIndex === line.sourceLineIndex).length;
      if (groupSize <= 1)
        return;

      patchState(store, { lines: storeLines.filter((l) => l.rowId !== rowId) });
    };

    const confirm = rxMethod<ReviewEditableLine[]>(
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

          const request = buildConfirmRequest(store.supplierReference(), store.note(), lines);

          return http.confirm(importId, request).pipe(
            mapResponse({
              next: (receipt) => {
                patchState(store, { confirmedReceipt: receipt });
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

    return { load, splitLine, removeLine, confirm };
  })
);

function buildConfirmRequest(
  supplierReference: string | null,
  note: string,
  lines: ReviewEditableLine[]
): ConfirmGoodsReceiptImportRequest {
  return {
    supplierReference,
    note,
    lines: lines.map(
      (line): ConfirmGoodsReceiptImportLineRequest => ({
        itemId: line.item!.id!,
        name: null,
        sku: null,
        unit: null,
        categoryName: null,
        isPerishable: isLinePerishable(line),
        locationId: line.location!.id!,
        quantity: line.quantity,
        expiryDate: toDateOnlyString(line.expiryDate),
        unitPrice: line.unitPrice,
        sourceLineIndex: line.sourceLineIndex
      })
    )
  };
}
