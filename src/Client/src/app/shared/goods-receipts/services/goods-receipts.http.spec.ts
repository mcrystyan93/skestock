import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { GoodsReceiptsHttp } from './goods-receipts.http';

describe('GoodsReceiptsHttp snapshot lines', () => {
  let http: HttpTestingController;
  let service: GoodsReceiptsHttp;
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
    service = TestBed.inject(GoodsReceiptsHttp);
  });
  afterEach(() => http.verify());

  it('reads saved lines by receipt id as a complete array', () => {
    let response: unknown;
    const rows = [{ id: 'snapshot-1', importId: 'import-1', rawItemText: 'Invoice label',
      quantity: 8, unitPrice: 3.25, lineTotal: 26, matchedItemId: null,
      locationId: null, locationName: null, expiryDate: '2027-02-01' }];
    service.getImportLines('receipt-1').subscribe(value => response = value);
    const request = http.expectOne('/api/GoodsReceipts/receipt-1/import-lines');
    expect(request.request.method).toBe('GET');
    request.flush(rows);
    expect(response).toEqual(rows);
  });
  it('returns an empty array without a stock-batch fallback', () => {
    let response: unknown;
    service.getImportLines('manual-receipt').subscribe(value => response = value);
    http.expectOne('/api/GoodsReceipts/manual-receipt/import-lines').flush([]);
    expect(response).toEqual([]);
  });
  it('propagates a failed request', () => {
    let status: number | undefined;
    service.getImportLines('missing').subscribe({ error: error => status = error.status });
    http.expectOne('/api/GoodsReceipts/missing/import-lines')
      .flush({ status: 404 }, { status: 404, statusText: 'Not Found' });
    expect(status).toBe(404);
  });
});
