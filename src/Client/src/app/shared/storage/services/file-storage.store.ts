import { patchState, signalStore, type, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { computed, inject } from '@angular/core';
// noinspection ES6PreferShortImport
import { StorageHttp } from '../services/storage.http';
import { FileMetadataDto } from '@ske/models';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { catchError, concatMap, from, map, of, pipe, switchMap, tap, toArray } from 'rxjs';
import { mapResponse } from '@ngrx/operators';
import { NzUploadFile } from 'ng-zorro-antd/upload';
import { eventGroup, injectDispatch } from '@ngrx/signals/events';

const PERCENT_MAX = 100;

type FailedUpload = { name: string };

type FileStorageState = {
  uploadedFiles: FileMetadataDto[];
  failedFiles: FailedUpload[];
  totalCount: number;
  completedCount: number;
  currentFileName: string | null;
};
const initialState: FileStorageState = {
  uploadedFiles: [],
  failedFiles: [],
  totalCount: 0,
  completedCount: 0,
  currentFileName: null
};
export const fileStorageApiEvents = eventGroup({
  source: 'FileUpload API',
  events: {
    uploadSuccess: type<FileMetadataDto[]>()
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
  withComputed((store) => ({
    uploadPercent: computed(() => {
      const total = store.totalCount();

      if (total === 0)
        return 0;

      return Math.round((store.completedCount() / total) * PERCENT_MAX);
    }),
    hasUploadFailures: computed(() => store.failedFiles().length > 0)
  })),
  withMethods((store) => {
    const uploadFiles = rxMethod<Array<NzUploadFile>>(
      pipe(
        tap((files) => {
          store.setUploadLoading();
          store.clearUploadErrors();
          patchState(store, {
            uploadedFiles: [],
            failedFiles: [],
            totalCount: files.length,
            completedCount: 0,
            currentFileName: null
          });
        }),
        switchMap((files) =>
          from(files).pipe(
            concatMap((file) => {
              patchState(store, { currentFileName: file.name });

              return store.storageHttp.requestUpload({ fileName: file.name, contentType: file.type! })
                .pipe(
                  concatMap((uploadResult) =>
                    store.storageHttp.uploadViaSasUri(file, uploadResult)
                      .pipe(map(() => uploadResult))
                  ),
                  concatMap((uploadResult) =>
                    store.storageHttp.confirmUpload({ fileId: uploadResult.fileId })
                  ),
                  tap((fileMetadata) => {
                    patchState(store, {
                      uploadedFiles: [...store.uploadedFiles(), fileMetadata],
                      completedCount: store.completedCount() + 1
                    });
                  }),
                  catchError(() => {
                    patchState(store, {
                      failedFiles: [...store.failedFiles(), { name: file.name }],
                      completedCount: store.completedCount() + 1
                    });

                    return of(null);
                  })
                );
            }),
            toArray()
          )
        ),
        tap(() => {
          patchState(store, { currentFileName: null });

          const uploaded = store.uploadedFiles();

          if (uploaded.length > 0)
            store.dispatcher.uploadSuccess(uploaded);

          store.setUploadLoaded();
        })
      )
    );

    const resetUpload = () => {
      patchState(store, {
        uploadedFiles: [],
        failedFiles: [],
        totalCount: 0,
        completedCount: 0,
        currentFileName: null
      });
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

    return { uploadFiles, resetUpload, downloadFile };
  })
);
