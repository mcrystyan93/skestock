import {inject} from '@angular/core';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {mapResponse} from '@ngrx/operators';
import {patchState, signalStore, withMethods, withProps, withState} from '@ngrx/signals';
import {concatMap, from, pipe, tap, toArray} from 'rxjs';
import {FileMetadataDto, GoodsReceiptImportDto} from '@ske/models';
import {withLoadingFeature} from '@ske/shared/loader';
import {withProblemDetailsFeature} from '@ske/shared/errors';
import {GoodsReceiptsHttp} from './goods-receipts.http';

export type GoodsReceiptImportBatch = {
  files: FileMetadataDto[];
  classId: string | null;
};

export type GoodsReceiptImportModalData = {
  classId: string | null;
};

type GoodsReceiptImportStateModel = {
  goodsReceiptImports: GoodsReceiptImportDto[];
};

const initialState: GoodsReceiptImportStateModel = {
  goodsReceiptImports: []
};

export const GoodsReceiptImportState = signalStore(
  withState(initialState),
  withLoadingFeature('goodsReceiptImport'),
  withProblemDetailsFeature('goodsReceiptImport'),
  withProps(() => ({
    goodsReceiptsHttp: inject(GoodsReceiptsHttp)
  })),
  withMethods((store) => {
    const createImports = rxMethod<GoodsReceiptImportBatch>(
      pipe(
        tap(() => {
          store.clearGoodsReceiptImportErrors();
          store.setGoodsReceiptImportLoading();
          patchState(store, {goodsReceiptImports: []});
        }),
        concatMap(({files, classId}) => {
          const requests = classId === null
            ? []
            : files.map((file) => ({
              fileMetadataId: file.id,
              classId
            }));

          return from(requests).pipe(
            concatMap((request) => store.goodsReceiptsHttp.createImport(request)),
            toArray()
          );
        }),
        mapResponse({
          next: (goodsReceiptImports) => {
            patchState(store, {goodsReceiptImports});
            store.setGoodsReceiptImportLoaded();
          },
          error: (error) => {
            store.handleGoodsReceiptImportError(error);
            store.setGoodsReceiptImportLoaded();
          }
        })
      )
    );

    return {createImports};
  })
);
