/**
 * Stable, machine-readable error codes emitted by the backend inside
 * `ProblemDetails.error.code` and `ProblemDetails.error.errors[].code`.
 *
 * Mirrors the C# source of truth:
 *  - `Application/Common/Errors/*Errors.cs` (resource / domain errors)
 *  - `Application/Common/Errors/ValidationErrorCodes.cs` (validation codes)
 *
 * Keep this in sync with the backend. Values are the exact wire codes.
 */
export const ErrorCodes = {
  // ── Common / cross-cutting errors ────────────────────────────────────────
  common: {
    notFound: 'common.not_found',
    unexpected: 'common.unexpected',
  },

  // ── Authentication / authorization errors ────────────────────────────────
  auth: {
    unauthorized: 'auth.unauthorized',
    forbidden: 'auth.forbidden',
  },

  // ── Resource / domain errors ─────────────────────────────────────────────
  resource: {
    categoryNotFound: 'categories.not_found',
    itemNotFound: 'items.not_found',
    locationNotFound: 'locations.not_found',
    schoolClassNotFound: 'school_classes.not_found',
    goodsReceiptNotFound: 'goods_receipts.not_found',
    goodsReceiptImportNotFound: 'goods_receipt_imports.not_found',
    goodsReceiptImportNotInReview: 'goods_receipt_imports.not_in_review',
    itemImportFileNotConfirmed: 'item_imports.file_not_confirmed',
    itemImportNotFound: 'item_imports.not_found',
    itemImportNotInReview: 'item_imports.not_in_review',
    itemImportRowsWithoutCategory: 'item_imports.rows_without_category',
    itemImportCategoriesNotFound: 'item_imports.categories_not_found',
    itemImportItemsNotFound: 'item_imports.items_not_found',
    itemImportDuplicateItems: 'item_imports.duplicate_items',
    itemImportItemCategoryMismatch: 'item_imports.item_category_mismatch',
    fileNotFound: 'storage.file_not_found',
    blobNotFound: 'storage.blob_not_found',
  },

  // ── Validation errors ────────────────────────────────────────────────────
  validation: {
    // Top-level / request-shape codes (emitted by the exception handler)
    failed: 'validation.failed',
    invalidRequest: 'validation.invalid_request',
    invalidJson: 'validation.invalid_json',
    invalidType: 'validation.invalid_type',
    invalidFormat: 'validation.invalid_format',
    outOfRange: 'validation.out_of_range',

    // Standard built-in validators
    required: 'validation.required',
    maxLength: 'validation.max_length',
    minLength: 'validation.min_length',
    email: 'validation.email',
    invalidEnum: 'validation.invalid_enum',
    greaterThan: 'validation.greater_than',
    greaterThanOrEqualTo: 'validation.greater_than_or_equal_to',
    lessThan: 'validation.less_than',
    lessThanOrEqualTo: 'validation.less_than_or_equal_to',
    between: 'validation.between',

    // Pagination / query codes
    invalidSortKey: 'validation.invalid_sort_key',
    invalidSortDirection: 'validation.invalid_sort_direction',
    invalidCursor: 'validation.invalid_cursor',
    cursorSortMismatch: 'validation.cursor_sort_mismatch',

    // Domain-specific uniqueness codes
    duplicateName: 'validation.duplicate_name',
    duplicateSku: 'validation.duplicate_sku',

    // Domain-specific reference codes
    invalidReference: 'validation.invalid_reference',
    selfReference: 'validation.self_reference',
    circularReference: 'validation.circular_reference',
    invalidDateRange: 'validation.invalid_date_range',

    // Goods receipt codes
    emptyLines: 'validation.empty_lines',
    duplicateReceiptLine: 'validation.duplicate_receipt_line',
    expiryDateRequired: 'validation.expiry_date_required',
    expiryDateNotAllowed: 'validation.expiry_date_not_allowed',
    splitQuantityMismatch: 'validation.split_quantity_mismatch',

    // Stock adjustment codes
    noAdjustmentNeeded: 'validation.no_adjustment_needed',

    // Fallback
    unknown: 'validation.unknown',
  },
} as const;

type ValueOf<T> = T[keyof T];

/** Union of every known common error code. */
export type CommonErrorCode = ValueOf<typeof ErrorCodes.common>;

/** Union of every known auth error code. */
export type AuthErrorCode = ValueOf<typeof ErrorCodes.auth>;

/** Union of every known resource error code. */
export type ResourceErrorCode = ValueOf<typeof ErrorCodes.resource>;

/** Union of every known validation error code. */
export type ValidationErrorCode = ValueOf<typeof ErrorCodes.validation>;

/** Union of every known backend error code. */
export type ErrorCode =
  | CommonErrorCode
  | AuthErrorCode
  | ResourceErrorCode
  | ValidationErrorCode;
