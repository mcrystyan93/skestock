import type { CategoryDto } from './category';

export type CategoryImportStatus = 'processing' | 'pendingReview' | 'confirmed' | 'failed';

export type CreateCategoryImportRequest = {
  fileMetadataId: string;
};

export type CategoryImportDto = {
  id: string;
  fileMetadataId: string;
  status: CategoryImportStatus;
  uploadedAt: string;
  processedAt?: string | null;
  errorMessage?: string | null;
};

export type CategoryImportSuggestionDto = {
  name: string;
  alreadyExists: boolean;
};

export type CategoryImportReviewDto = {
  id: string;
  status: CategoryImportStatus;
  errorMessage?: string | null;
  uploadedAt: string;
  processedAt?: string | null;
  suggestions: CategoryImportSuggestionDto[];
};

export type ConfirmCategoryImportRequest = {
  names: string[];
};

export type ConfirmCategoryImportResponse = {
  importId: string;
  status: CategoryImportStatus;
  categories: Array<{
    id: string;
    name: string;
    created: boolean;
  }>;
};
