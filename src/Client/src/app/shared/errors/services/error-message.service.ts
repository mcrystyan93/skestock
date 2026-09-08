import { Service } from '@angular/core';
import { BackendErrorItem, ErrorParams, ProblemDetails } from '@ske/models';
import { RO_DEFAULT_ERROR_MESSAGE, RO_ERROR_MESSAGES } from './error-messages.ro';

/**
 * Resolves stable backend error codes to human-readable Romanian phrases.
 *
 * The backend surfaces codes in `ProblemDetails.error.code` and per-field in
 * `ProblemDetails.error.errors[].code`, optionally with camelCase interpolation
 * `params` (e.g. `maxLength`, `comparisonValue`). This service maps those to
 * localized copy, interpolating params where the phrase supports it.
 */
@Service()
export class ErrorMessageService {
  /**
   * Resolves a raw error `code` (+ optional `params`) to a Romanian phrase.
   * Unknown or empty codes fall back to a generic message.
   */
  public resolveCode(code: string | null | undefined, params?: ErrorParams | null): string {
    if (!code) return RO_DEFAULT_ERROR_MESSAGE;

    const entry = RO_ERROR_MESSAGES[code];
    if (entry === undefined) return RO_DEFAULT_ERROR_MESSAGE;

    return typeof entry === 'function' ? entry(params ?? {}) : entry;
  }

  /**
   * Resolves a single `BackendErrorItem` (from `error.errors[]`) to a Romanian
   * phrase, using its own `code` and `params`.
   */
  public resolveItem(item: BackendErrorItem): string {
    return this.resolveCode(item?.code, item?.params);
  }

  /**
   * Resolves the top-level phrase for a `ProblemDetails`. Prefers the primary
   * `error.code`, borrowing the first field item's `params` for interpolation
   * when the top-level payload doesn't carry any. Falls back to the raw
   * `detail`/`title` when no code is present.
   */
  public resolveProblem(problem: ProblemDetails | null | undefined): string {
    if (!problem) return RO_DEFAULT_ERROR_MESSAGE;

    const code = problem.error?.code;
    if (code) {
      const params = problem.error?.errors?.[0]?.params ?? undefined;
      return this.resolveCode(code, params);
    }

    return problem.detail ?? problem.title ?? RO_DEFAULT_ERROR_MESSAGE;
  }
}
