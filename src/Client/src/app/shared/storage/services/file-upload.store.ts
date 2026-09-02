import { patchState, signalStore, type, withMethods, withProps, withState } from '@ngrx/signals';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { inject } from '@angular/core';
import { StorageHttp } from '@ske/shared/storage';
import { FileMetadataDto } from '@ske/models';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { concatMap, map, pipe, tap } from 'rxjs';
import { mapResponse } from '@ngrx/operators';
import { NzUploadFile } from 'ng-zorro-antd/upload';
import { eventGroup, injectDispatch } from '@ngrx/signals/events';

type FileUploadState = { fileMetadata: FileMetadataDto | null };
const initialState: FileUploadState = { fileMetadata: null };
export const fileUploadApiEvents = eventGroup({
  source: 'FileUpload API',
  events: {
    uploadSuccess: type<FileMetadataDto>()
  }
});
export const FileUploadState = signalStore(
  withState(initialState),
  withLoadingFeature('upload'),
  withProblemDetailsFeature('upload'),
  withProps(() => ({
    storageHttp: inject(StorageHttp),
    dispatcher: injectDispatch(fileUploadApiEvents)
  })),
  withMethods((store) => {
    const uploadFile = rxMethod<NzUploadFile>(
      pipe(
        tap(() => {
          store.setUploadLoading();
          store.clearUploadErrors();
          patchState(store, { fileMetadata: null });
        }),
        concatMap((file) =>
          store.storageHttp.requestUpload({ fileName: file.name, contentType: file.type! })
            .pipe(
              map(uploadResult => ({ file, uploadResult }))
            )
        ),
        concatMap(({ uploadResult, file }) =>
          store.storageHttp.uploadViaSasUri(file, uploadResult)
            .pipe(
              map(() => uploadResult)
            )
        ),
        concatMap((uploadResult) =>
          store.storageHttp.confirmUpload({ fileId: uploadResult.fileId })),
        mapResponse({
          next: (fileMetadata) => {
            patchState(store, { fileMetadata });

            store.dispatcher.uploadSuccess(fileMetadata);

            store.setUploadLoaded();
          },
          error: (error) => {
            store.handleUploadError(error);
            store.setUploadLoaded();
          }
        })
      )
    );

    const resetUpload = () => {
      patchState(store, { fileMetadata: null });
      store.clearUploadErrors();
    };

    return { uploadFile, resetUpload };
  })
);
