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

type FileStorageState = { fileMetadata: FileMetadataDto | null };
const initialState: FileStorageState = { fileMetadata: null };
export const fileStorageApiEvents = eventGroup({
  source: 'FileUpload API',
  events: {
    uploadSuccess: type<FileMetadataDto>()
  }
});

function triggerBrowserDownload(blob: Blob, fileName: string): void {
  const objectUrl = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = objectUrl;
  anchor.download = fileName;
  document.body.appendChild(anchor);
  anchor.click();
  document.body.removeChild(anchor);
  URL.revokeObjectURL(objectUrl);
}

export const FileStorageState = signalStore(
  withState(initialState),
  withLoadingFeature('upload'),
  withProblemDetailsFeature('upload'),
  withLoadingFeature('download'),
  withProblemDetailsFeature('download'),
  withProps(() => ({
    storageHttp: inject(StorageHttp),
    dispatcher: injectDispatch(fileStorageApiEvents)
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

    const downloadFile = rxMethod<string>(
      pipe(
        tap(() => {
          store.setDownloadLoading();
          store.clearDownloadErrors();
        }),
        concatMap((fileId) =>
          store.storageHttp.getFileDownload(fileId)
            .pipe(
              concatMap((downloadResult) =>
                store.storageHttp.downloadBlob(downloadResult.downloadUrl)
                  .pipe(map((blob) => ({ downloadResult, blob })))
              )
            )
        ),
        mapResponse({
          next: ({ downloadResult, blob }) => {
            triggerBrowserDownload(blob, downloadResult.file.originalName);
            store.setDownloadLoaded();
          },
          error: (error) => {
            store.handleDownloadError(error);
            store.setDownloadLoaded();
          }
        })
      )
    );

    return { uploadFile, resetUpload, downloadFile };
  })
);
