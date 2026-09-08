import {HttpClient, HttpHeaders} from '@angular/common/http';
import {inject, Service} from '@angular/core';
import {ConfirmUploadRequest, FileDownloadResult, FileMetadataDto, RequestUploadRequest, UploadRequestResult} from '@ske/models';
import {NzUploadFile} from 'ng-zorro-antd/upload';

/**
 * HTTP client for src/Web/Endpoints/Storage.cs, mapped under /api/Storage.
 */
@Service()
export class StorageHttp {
  private readonly _httpClient = inject(HttpClient);

  public requestUpload(request: RequestUploadRequest) {
    return this._httpClient.post<UploadRequestResult>('/api/Storage/request-upload', request);
  }

  public uploadViaSasUri(file: NzUploadFile, uploadResult: UploadRequestResult) {
    const headers = new HttpHeaders({
      'x-ms-blob-type': 'BlockBlob',
      'Content-Type': file.type!
    });

    return this._httpClient.put(uploadResult.uploadUrl, file, {
      headers
    });
  }

  public confirmUpload(request: ConfirmUploadRequest) {
    return this._httpClient.post<FileMetadataDto>('/api/Storage/confirm-upload', request);
  }

  public getFileDownload(id: string) {
    return this._httpClient.get<FileDownloadResult>(`/api/Storage/${id}/download`);
  }

  public downloadBlob(sasUrl: string) {
    return this._httpClient.get(sasUrl, {responseType: 'blob'});
  }
}
