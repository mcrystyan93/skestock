import { TestBed } from '@angular/core/testing';
import { GoodsReceiptImportLineDto } from '@ske/models';
import { ImportLines } from './import-lines';

const line: GoodsReceiptImportLineDto = {
  id: 'saved-1', importId: 'import-1', rawItemText: 'Original invoice name', matchedItemId: null,
  locationId: null, locationName: null, quantity: 8, unitPrice: 3.25, lineTotal: 26, expiryDate: null
};

describe('ImportLines', () => {
  beforeEach(() => TestBed.configureTestingModule({ imports: [ImportLines] }));

  it('renders saved labels and preserves split rows in both layouts', () => {
    const fixture = TestBed.createComponent(ImportLines);
    fixture.componentRef.setInput('lines', [line, { ...line, id: 'saved-2', quantity: 4, lineTotal: 13 }]);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelectorAll('tbody tr:not([nz-table-measure-row])').length).toBe(2);
    expect(element.querySelectorAll('article').length).toBe(2);
    expect(element.textContent).toContain('Original invoice name');
    expect(element.textContent).toContain('Locație nespecificată');
    expect(element.querySelector('tbody')?.textContent).toContain('8');
    expect(element.querySelector('tbody')?.textContent).toContain('26');
  });

  it('shows the saved-lines empty state without a loading skeleton', () => {
    const fixture = TestBed.createComponent(ImportLines);
    fixture.componentRef.setInput('lines', []);
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent)
      .toContain('Nu există linii salvate pentru această recepție');
    expect((fixture.nativeElement as HTMLElement).querySelector('nz-skeleton')).toBeNull();
  });

  it('shows loading instead of a successful empty state', () => {
    const fixture = TestBed.createComponent(ImportLines);
    fixture.componentRef.setInput('lines', []);
    fixture.componentRef.setInput('loading', true);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('[aria-busy="true"]')).not.toBeNull();
    expect(element.textContent).not.toContain('Nu există linii salvate');
  });
});
