import { TestBed } from '@angular/core/testing';
import { BackendErrorItem, ErrorCodes, ProblemDetails } from '@ske/models';
import { ErrorMessageService } from './error-message.service';
import { RO_DEFAULT_ERROR_MESSAGE } from './error-messages.ro';

describe('ErrorMessageService', () => {
  let service: ErrorMessageService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ErrorMessageService);
  });

  it('resolves a known static resource code to Romanian copy', () => {
    expect(service.resolveCode(ErrorCodes.resource.categoryNotFound))
      .toBe('Categoria nu a fost găsită.');
  });

  it('interpolates params for a maxLength validation code', () => {
    const message = service.resolveCode(ErrorCodes.validation.maxLength, { maxLength: 50 });
    expect(message).toContain('50');
    expect(message).toContain('caractere');
  });

  it('interpolates a between validation code with from/to', () => {
    const message = service.resolveCode(ErrorCodes.validation.between, { from: 1, to: 10 });
    expect(message).toContain('1');
    expect(message).toContain('10');
  });

  it('falls back gracefully when an interpolated code has no params', () => {
    const message = service.resolveCode(ErrorCodes.validation.maxLength);
    expect(message).toBeTruthy();
    expect(message).not.toContain('undefined');
  });

  it('returns the default message for an unknown code', () => {
    expect(service.resolveCode('some.unknown.code')).toBe(RO_DEFAULT_ERROR_MESSAGE);
  });

  it('resolves common and auth codes', () => {
    expect(service.resolveCode(ErrorCodes.common.unexpected)).toContain('eroare neașteptată');
    expect(service.resolveCode(ErrorCodes.auth.forbidden)).toContain('permisiunea');
    expect(service.resolveCode(ErrorCodes.validation.invalidJson)).toContain('JSON');
  });

  it('returns the default message for a null/empty code', () => {
    expect(service.resolveCode(null)).toBe(RO_DEFAULT_ERROR_MESSAGE);
    expect(service.resolveCode('')).toBe(RO_DEFAULT_ERROR_MESSAGE);
  });

  it('resolves a BackendErrorItem via resolveItem', () => {
    const item: BackendErrorItem = {
      field: 'name',
      code: ErrorCodes.validation.minLength,
      params: { minLength: 3 },
    };
    expect(service.resolveItem(item)).toContain('3');
  });

  it('resolves a ProblemDetails via its top-level error code', () => {
    const problem: ProblemDetails = {
      status: 404,
      error: { code: ErrorCodes.resource.itemNotFound },
    };
    expect(service.resolveProblem(problem)).toBe('Articolul nu a fost găsit.');
  });

  it('borrows field params when resolving a ProblemDetails', () => {
    const problem: ProblemDetails = {
      status: 400,
      error: {
        code: ErrorCodes.validation.maxLength,
        errors: [{ field: 'name', code: ErrorCodes.validation.maxLength, params: { maxLength: 25 } }],
      },
    };
    expect(service.resolveProblem(problem)).toContain('25');
  });

  it('falls back to detail/title when a ProblemDetails has no code', () => {
    expect(service.resolveProblem({ status: 500, detail: 'Boom' })).toBe('Boom');
    expect(service.resolveProblem(null)).toBe(RO_DEFAULT_ERROR_MESSAGE);
  });
});
