import { TestBed } from '@angular/core/testing';
import { GetAllGoodsReceiptImportsRequest, GoodsReceiptImportListItemDto } from '@ske/models';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { GoodsReceiptImportsListSmall } from './goods-receipt-imports-list-small';

describe('GoodsReceiptImportsListSmall', () => {
  const filter: GetAllGoodsReceiptImportsRequest = {
    filters: [],
    cursor: null,
    pageSize: 50,
    sort: []
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [GoodsReceiptImportsListSmall],
      providers: [
        provideNzIconsTesting(),
        provideNzIcons([{ name: 'icons:download', icon: '<svg viewBox="0 0 24 24"></svg>' }])
      ]
    });
  });

  it('shows skeleton cards while loading the first page', () => {
    const fixture = createFixture([], true);
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelectorAll('nz-skeleton').length).toBe(4);
    expect(element.querySelector('nz-empty')).toBeNull();
    fixture.destroy();
  });

  it('shows the Romanian empty state when there are no imports', () => {
    const fixture = createFixture();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Nu există importuri');
    fixture.destroy();
  });

  it('opens review from a pending-review card and emits download', () => {
    const importDto = createImport();
    const fixture = createFixture([importDto]);
    const component = fixture.componentInstance;
    const review = vi.spyOn(component.review, 'emit');
    const download = vi.spyOn(component.downloadFile, 'emit');
    const element = fixture.nativeElement as HTMLElement;

    expect(element.textContent).toContain('aviz-clasa-5.xlsx');
    element.querySelector<HTMLButtonElement>('button[aria-label="Revizuiește importul aviz-clasa-5.xlsx"]')!.click();
    element.querySelector<HTMLButtonElement>('button[aria-label="Descarcă fișierul"]')!.click();

    expect(review).toHaveBeenCalledWith(importDto);
    expect(download).toHaveBeenCalledWith(importDto);
    fixture.destroy();
  });

  function createFixture(items: GoodsReceiptImportListItemDto[] = [], loading = false) {
    const fixture = TestBed.createComponent(GoodsReceiptImportsListSmall);
    fixture.componentRef.setInput('items', items);
    fixture.componentRef.setInput('filter', filter);
    fixture.componentRef.setInput('loading', loading);
    fixture.componentRef.setInput('hasNextPage', true);
    fixture.componentRef.setInput('isLoadingMore', false);
    fixture.detectChanges();
    return fixture;
  }
});

function createImport(overrides: Partial<GoodsReceiptImportListItemDto> = {}): GoodsReceiptImportListItemDto {
  return {
    id: 'import-1',
    status: 'pendingReview',
    classId: 'class-1',
    className: 'Clasa a V-a',
    fileMetadataId: 'file-1',
    blobPath: 'class-1/aviz-clasa-5.xlsx',
    uploadedByName: 'Administrator',
    uploadedAt: '2026-09-29T10:00:00Z',
    createdDate: '2026-09-29T10:00:00Z',
    ...overrides
  };
}
