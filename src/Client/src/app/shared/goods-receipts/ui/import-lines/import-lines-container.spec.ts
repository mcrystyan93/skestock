import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ImportLinesContainer } from './import-lines-container';

const snapshot = { id: 'line-1', importId: 'import-1', rawItemText: 'Saved invoice text',
  matchedItemId: null, locationId: null, locationName: null, quantity: 8, unitPrice: 3.25,
  lineTotal: 26, expiryDate: null };

describe('ImportLinesContainer', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [ImportLinesContainer], providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('loads on mount and reloads when the receipt input changes', () => {
    const fixture = TestBed.createComponent(ImportLinesContainer);
    fixture.componentRef.setInput('goodsReceiptId', 'receipt-1');
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('[aria-busy="true"]')).not.toBeNull();
    http.expectOne('/api/GoodsReceipts/receipt-1/import-lines').flush([snapshot]);
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Saved invoice text');
    fixture.componentRef.setInput('goodsReceiptId', 'manual');
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('Saved invoice text');
    http.expectOne('/api/GoodsReceipts/manual/import-lines').flush([]);
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Nu există linii salvate');
  });

  it('shows a failed load without an empty success and retries with the same receipt id', () => {
    const fixture = TestBed.createComponent(ImportLinesContainer);
    fixture.componentRef.setInput('goodsReceiptId', 'receipt-1');
    fixture.detectChanges();
    http.expectOne('/api/GoodsReceipts/receipt-1/import-lines')
      .flush({ status: 500, title: 'Failed' }, { status: 500, statusText: 'Error' });
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('ske-error-display')).not.toBeNull();
    expect(element.textContent).not.toContain('Nu există linii salvate');
    element.querySelector<HTMLButtonElement>('button')!.click();
    fixture.detectChanges();
    http.expectOne('/api/GoodsReceipts/receipt-1/import-lines').flush([snapshot]);
    fixture.detectChanges();
    expect(element.textContent).toContain('Saved invoice text');
    expect(element.querySelector('ske-error-display')).toBeNull();
  });
});
