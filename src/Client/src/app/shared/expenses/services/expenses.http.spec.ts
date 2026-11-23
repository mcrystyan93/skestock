import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ExpensesHttp } from './expenses.http';

describe('ExpensesHttp', () => {
  let http: HttpTestingController;
  let service: ExpensesHttp;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
    service = TestBed.inject(ExpensesHttp);
  });
  afterEach(() => http.verify());

  it('creates an expense from an uploaded document', () => {
    service.createFromDocument({ classId: 'c1', fileMetadataId: 'f1' }).subscribe();

    const request = http.expectOne('/api/Expenses/from-document');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ classId: 'c1', fileMetadataId: 'f1' });
    request.flush({});
  });

  it('updates an expense with the editable fields only', () => {
    const fields = {
      supplierName: 'ACME', documentNumber: null, expenseDate: '2026-10-05', currency: 'RON',
      totalAmount: 119, vatAmount: 19, expenseCategoryId: null
    };
    service.update('e1', fields).subscribe();

    const request = http.expectOne('/api/Expenses/e1');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(fields);
    request.flush({});
  });

  it('retries and deletes by id', () => {
    service.retry('e1').subscribe();
    expect(http.expectOne('/api/Expenses/e1/retry').request.method).toBe('POST');

    service.delete('e2').subscribe();
    expect(http.expectOne('/api/Expenses/e2').request.method).toBe('DELETE');
  });

  it('propagates a failed request', () => {
    let status: number | undefined;
    service.getById('missing').subscribe({ error: error => status = error.status });

    http.expectOne('/api/Expenses/missing').flush({ status: 404 }, { status: 404, statusText: 'Not Found' });
    expect(status).toBe(404);
  });
});
