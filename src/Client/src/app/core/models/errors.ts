export type ErrorParams = Record<string, unknown>;

export type BackendErrorItem = {
  field?: string | null;
  code: string;
  params?: ErrorParams | null;
};

export type BackendErrorDiagnostics = {
  correlationId?: string | null;
};

export type BackendErrorPayload = {
  code: string;
  errors?: BackendErrorItem[] | null;
  diagnostics?: BackendErrorDiagnostics | null;
};

export type ProblemDetails = {
  type?: string;
  title?: string;
  status: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  error?: BackendErrorPayload;
};

export type ValidationProblemDetails = ProblemDetails & {
  error: BackendErrorPayload & { errors: BackendErrorItem[] };
};

// Type guard to safely identify validation errors in components.
export function isValidationProblem(error: unknown): error is ValidationProblemDetails {
  if (!error || typeof error !== 'object') return false;

  const candidate = error as Partial<ValidationProblemDetails>;
  const items = candidate.error?.errors;

  return candidate.status === 400 && Array.isArray(items) && items.some((item) => !!item?.field);
}

