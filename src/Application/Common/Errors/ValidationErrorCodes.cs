namespace skestock.Application.Common.Errors;

public class ValidationErrorCodes
{
    // ── Standard built-in validators ───────────────────────────────────────
    public const string Required = "validation.required";
    public const string MaxLength = "validation.max_length";
    public const string MinLength = "validation.min_length";
    public const string Email = "validation.email";
    public const string InvalidEnum = "validation.invalid_enum";
    public const string GreaterThan = "validation.greater_than";
    public const string GreaterThanOrEqualTo = "validation.greater_than_or_equal_to";
    public const string LessThan = "validation.less_than";
    public const string LessThanOrEqualTo = "validation.less_than_or_equal_to";
    public const string Between = "validation.between";

    // ── Pagination / query codes ───────────────────────────────────────────
    public const string InvalidSortKey = "validation.invalid_sort_key";
    public const string InvalidSortDirection = "validation.invalid_sort_direction";
    public const string InvalidCursor = "validation.invalid_cursor";
    public const string CursorSortMismatch = "validation.cursor_sort_mismatch";

    // ── Domain-specific uniqueness codes ───────────────────────────────────
    public const string DuplicateName = "validation.duplicate_name";
    /// <summary>A SKU (stock keeping unit) value already exists on another item.</summary>
    public const string DuplicateSku = "validation.duplicate_sku";

    // ── Domain-specific reference codes ────────────────────────────────────
    /// <summary>A referenced foreign-key id (e.g. ParentLocationId) does not exist.</summary>
    public const string InvalidReference = "validation.invalid_reference";
    /// <summary>An entity references itself where a different entity is required (e.g. a location as its own parent).</summary>
    public const string SelfReference = "validation.self_reference";
    /// <summary>Applying the requested change would create a circular reference (e.g. in a self-referencing hierarchy).</summary>
    public const string CircularReference = "validation.circular_reference";
    /// <summary>A date range is invalid (e.g. StartDate is not strictly before EndDate).</summary>
    public const string InvalidDateRange = "validation.invalid_date_range";

    // ── Goods receipt codes ────────────────────────────────────────────────
    /// <summary>A goods receipt was submitted with no lines.</summary>
    public const string EmptyLines = "validation.empty_lines";
    /// <summary>Two or more lines in the same receipt reference the same item/location/expiry combination.</summary>
    public const string DuplicateReceiptLine = "validation.duplicate_receipt_line";
    /// <summary>A perishable item's receipt line is missing its expiry date.</summary>
    public const string ExpiryDateRequired = "validation.expiry_date_required";
    /// <summary>A non-perishable item's receipt line supplied an expiry date, which isn't allowed.</summary>
    public const string ExpiryDateNotAllowed = "validation.expiry_date_not_allowed";
    /// <summary>The split lines derived from one extracted import line don't sum back to its original quantity.</summary>
    public const string SplitQuantityMismatch = "validation.split_quantity_mismatch";

    // ── Stock adjustment codes ─────────────────────────────────────────────
    /// <summary>The counted quantity submitted for a stock adjustment equals the current total, so nothing would change.</summary>
    public const string NoAdjustmentNeeded = "validation.no_adjustment_needed";

    // ── Fallback ───────────────────────────────────────────────────────────
    public const string Unknown = "validation.unknown";
}
