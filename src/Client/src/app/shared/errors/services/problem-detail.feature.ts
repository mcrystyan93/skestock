import {
  patchState,
  signalStoreFeature,
  withComputed,
  withMethods,
  withState,
} from '@ngrx/signals';
import {isNil} from 'lodash-es';
// Helper types for prefixed keys
import { isValidationProblem, ProblemDetails, ValidationProblemDetails } from '@ske/models';
import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';

type ProblemDetailKey<P extends string> = `${P}ProblemDetail`;
type ValidationErrorsKey<P extends string> = `${P}ValidationErrors`;
type HandleErrorKey<P extends string> = `handle${Capitalize<P>}Error`;
type ClearErrorsKey<P extends string> = `clear${Capitalize<P>}Errors`;
type ErrorStatusKey<P extends string> = `${P}ErrorStatus`;

type ProblemDetailsState<P extends string> =
  Record<ProblemDetailKey<P>, ProblemDetails | null> &
  Record<ValidationErrorsKey<P>, ValidationProblemDetails | null>;

export type ErrorStatus = 'error' | '404' | '403' | '500';

export function withProblemDetailsFeature<P extends string>(prefix: P) {
  const capitalized = (prefix.charAt(0).toUpperCase() + prefix.slice(1)) as Capitalize<P>;

  const problemDetailKey = `${prefix}ProblemDetail` as ProblemDetailKey<P>;
  const validationErrorsKey = `${prefix}ValidationErrors` as ValidationErrorsKey<P>;
  const handleErrorKey = `handle${capitalized}Error` as HandleErrorKey<P>;
  const clearErrorsKey = `clear${capitalized}Errors` as ClearErrorsKey<P>;
  const errorStatusKey = `${prefix}ErrorStatus` as ErrorStatusKey<P>;

  return signalStoreFeature(
    withState({
      [problemDetailKey]: null,
      [validationErrorsKey]: null,
    } as ProblemDetailsState<P>),
    withMethods((store) => {
      const methods = {
        [handleErrorKey](error: unknown) {
          if (error instanceof HttpErrorResponse) error = error.error;

          if (isValidationProblem(error)) {
            patchState(store, {[validationErrorsKey]: error, [problemDetailKey]: null} as any);
          } else if (error && typeof error === 'object' && 'status' in error) {
            patchState(store, {[problemDetailKey]: error as ProblemDetails, [validationErrorsKey]: null} as any);
          }
        },
        [clearErrorsKey]() {
          patchState(store, {[validationErrorsKey]: null, [problemDetailKey]: null} as any);
        },
      };

      return methods as
        Record<HandleErrorKey<P>, (error: unknown) => void> &
        Record<ClearErrorsKey<P>, () => void>;
    }),
    withComputed((store) => {
      const computed = {
        [errorStatusKey]: (): ErrorStatus => {
          const s = store as Record<string, () => ProblemDetails | ValidationProblemDetails | null>;
          const error = s[problemDetailKey]() ?? s[validationErrorsKey]();

          if (isNil(error)) return 'error';
          switch (error.status) {
            case HttpStatusCode.NotFound: return '404';
            case HttpStatusCode.Forbidden: return '403';
            case HttpStatusCode.InternalServerError: return '500';
            default: return 'error';
          }
        },
      };

      return computed as Record<ErrorStatusKey<P>, () => ErrorStatus>;
    }),
  );
}
