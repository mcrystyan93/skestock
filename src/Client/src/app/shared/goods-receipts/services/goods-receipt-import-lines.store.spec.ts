import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { GoodsReceiptImportLinesStore } from './goods-receipt-import-lines.store';

const snapshot = {
  id: 'line-1', importId: 'import-1', rawItemText: 'Saved label', matchedItemId: null,
  locationId: null, locationName: null, quantity: 8, unitPrice: 3.25, lineTotal: 26, expiryDate: null
};

describe('GoodsReceiptImportLinesStore', () => {
  let store: InstanceType<typeof GoodsReceiptImportLinesStore>;
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), GoodsReceiptImportLinesStore] });
    store = TestBed.inject(GoodsReceiptImportLinesStore);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('loads saved rows and distinguishes empty success from loading', () => {
    store.load('receipt-1');
    expect(store.linesLoading()).toBe(true);
    http.expectOne('/api/GoodsReceipts/receipt-1/import-lines').flush([snapshot]);
    expect(store.lines()).toEqual([snapshot]);
    expect(store.linesLoading()).toBe(false);
    store.load('manual');
    expect(store.lines()).toEqual([]);
    http.expectOne('/api/GoodsReceipts/manual/import-lines').flush([]);
    expect(store.linesLoading()).toBe(false);
    expect(store.linesProblemDetail()).toBeNull();
  });

  it('cancels an obsolete receipt request and applies only the latest rows', () => {
    store.load('old');
    const old = http.expectOne('/api/GoodsReceipts/old/import-lines');
    store.load('new');
    expect(old.cancelled).toBe(true);
    http.expectOne('/api/GoodsReceipts/new/import-lines').flush([snapshot]);
    expect(store.receiptId()).toBe('new');
    expect(store.lines()).toEqual([snapshot]);
  });

  it('clears error state on retry and recovers for the current receipt', () => {
    store.load('receipt-1');
    http.expectOne('/api/GoodsReceipts/receipt-1/import-lines')
      .flush({ status: 500, title: 'Load failed' }, { status: 500, statusText: 'Error' });
    expect(store.linesLoading()).toBe(false);
    expect(store.linesProblemDetail()?.title).toBe('Load failed');
    store.retry();
    expect(store.linesProblemDetail()).toBeNull();
    expect(store.linesLoading()).toBe(true);
    http.expectOne('/api/GoodsReceipts/receipt-1/import-lines').flush([snapshot]);
    expect(store.lines()).toEqual([snapshot]);
  });

  it('isolates concurrent store instances', () => {
    const other = TestBed.runInInjectionContext(() => new GoodsReceiptImportLinesStore());
    store.load('first');
    other.load('second');
    http.expectOne('/api/GoodsReceipts/second/import-lines').flush([snapshot]);
    expect(store.linesLoading()).toBe(true);
    expect(store.lines()).toEqual([]);
    http.expectOne('/api/GoodsReceipts/first/import-lines').flush([]);
    expect(other.lines()).toEqual([snapshot]);
    expect(other.receiptId()).toBe('second');
  });
});
