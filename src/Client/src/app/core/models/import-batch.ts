import type { FileStatus } from './storage';

export type ImportBatchStatus = 'processing' | 'pendingReview' | 'confirmed' | 'failed' | 'all';

export type ImportBatchHistoryStatus = 'created' | 'processing' | 'completed' | 'failed' | 'confirmed';

export type ImportBatchHistoryDto = {
  status: ImportBatchHistoryStatus;
  attempt: number;
  message?: string | null;
  createdAtUtc: string;
};

export type ImportBatchFileDto = {
  fileMetadataId: string;
  originalName: string;
  blobPath: string;
  contentType: string;
  sizeBytes: number;
  status: FileStatus;
  sortOrder: number;
};
