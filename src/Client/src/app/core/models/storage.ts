/**
 * Mirrors src/Domain/Enums/FileStatus.cs. JsonStringEnumConverter is registered for the API,
 * so this enum arrives as a lower-camel string on the wire.
 */
export type FileStatus = 'pending' | 'completed' | 'failed' | 'deleted';

/** Mirrors src/Application/Storage/Models/StorageRequests.cs. */
export type RequestUploadRequest = {
  fileName: string;
  contentType: string;
};

/** Mirrors src/Application/Storage/Models/StorageRequests.cs. */
export type ConfirmUploadRequest = {
  fileId: string;
};

/** Mirrors src/Application/Storage/Models/UploadRequestResult.cs. */
export type UploadRequestResult = {
  fileId: string;
  uploadUrl: string;
};

/** Mirrors src/Application/Storage/DTOs/FileMetadataDto.cs. */
export type FileMetadataDto = {
  id: string;
  fileId: string;
  originalName: string;
  blobContainer: string;
  blobPath: string;
  contentType: string;
  sizeBytes: number;
  status: FileStatus;
  eTag?: string | null;
  completedDate?: string | null;
};
