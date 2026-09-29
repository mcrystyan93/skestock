import { TestBed } from '@angular/core/testing';
import { GetAllGoodsReceiptsRequest, GoodsReceiptListItemDto } from '@ske/models';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { GoodsReceiptListSmall } from './goods-receipt-list-small';

describe('GoodsReceiptListSmall', () => {
  const filter: GetAllGoodsReceiptsRequest = {
    filters: [],
    cursor: null,
    pageSize: 50,
    sort: []
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [GoodsReceiptListSmall],
      providers: [
        provideNzIconsTesting(),
        provideNzIcons([
          { name: 'icons:chevron-down', icon: '<svg viewBox="0 0 24 24"></svg>' },
          { name: 'icons:download', icon: '<svg viewBox="0 0 24 24"></svg>' }
        ])
      ]
    });
  });

  it('shows skeleton cards while loading', () => {
    const fixture = createFixture([], true);
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelectorAll('nz-skeleton').length).toBe(4);
    expect(element.querySelector('nz-empty')).toBeNull();
    fixture.destroy();
  });

  it('shows a Romanian empty state when there are no receipts', () => {
    const fixture = createFixture();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Nu există comenzi recepționate');
    fixture.destroy();
  });

  it('emits download only through the file action', () => {
    const receipt = createReceipt();
    const fixture = createFixture([receipt]);
    const emit = vi.spyOn(fixture.componentInstance.downloadFile, 'emit');
    const element = fixture.nativeElement as HTMLElement;
    const downloadButton = element.querySelector<HTMLButtonElement>('button[aria-label="Descarcă fișierul"]')!;

    expect(downloadButton.disabled).toBe(false);
    downloadButton.click();
    expect(emit).toHaveBeenCalledWith(receipt);

    fixture.destroy();
  });

  it('disables file download when a receipt has no file', () => {
    const fixture = createFixture([createReceipt({ fileMetadataId: null })]);
    const button = (fixture.nativeElement as HTMLElement)
      .querySelector<HTMLButtonElement>('button[aria-label="Descarcă fișierul"]')!;

    expect(button.disabled).toBe(true);
    fixture.destroy();
  });

  it('toggles the expanded receipt state', () => {
    const fixture = createFixture([createReceipt()]);
    const component = fixture.componentInstance;

    component.toggleRow('receipt-1');
    expect(component.isExpanded('receipt-1')).toBe(true);
    component.toggleRow('receipt-1');
    expect(component.isExpanded('receipt-1')).toBe(false);
    fixture.destroy();
  });

  function createFixture(items: GoodsReceiptListItemDto[] = [], loading = false) {
    const fixture = TestBed.createComponent(GoodsReceiptListSmall);
    fixture.componentRef.setInput('items', items);
    fixture.componentRef.setInput('filter', filter);
    fixture.componentRef.setInput('loading', loading);
    fixture.componentRef.setInput('hasNextPage', true);
    fixture.componentRef.setInput('isLoadingMore', false);
    fixture.detectChanges();
    return fixture;
  }
});

function createReceipt(overrides: Partial<GoodsReceiptListItemDto> = {}): GoodsReceiptListItemDto {
  return {
    id: 'receipt-1',
    classId: 'class-1',
    className: 'Clasa a V-a',
    receivedAt: '2026-09-29T10:00:00Z',
    note: 'Aviz de papetărie',
    lineCount: 2,
    totalQuantity: 12,
    createdByName: 'Administrator',
    createdDate: '2026-09-29T10:00:00Z',
    totalAmount: 135.5,
    fileMetadataId: 'file-1',
    ...overrides
  };
}
