import { TestBed } from '@angular/core/testing';
import { NZ_MODAL_DATA, NzModalRef } from 'ng-zorro-antd/modal';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { groupStockByItem, StockItemDto } from '@ske/models';
import { StockLocationPickerModal } from './stock-location-picker-modal';

describe('StockLocationPickerModal', () => {
  const line = (locationId: string, locationName: string, overrides: Partial<StockItemDto> = {}): StockItemDto => ({
    itemId: 'item-1', itemName: 'Mere', categoryId: 'c', categoryName: 'Alimente', locationId, locationName,
    lastUpdatedAt: null, unit: 'kg', isPerishable: false, isExpired: false, expiredQuantity: 0, quantity: 4,
    isLowStock: false, hideWhenZeroStock: false, ...overrides
  });

  function create() {
    const close = vi.fn();
    const group = groupStockByItem([line('l1', 'Cămară', { isLowStock: true }), line('l2', 'Frigider')])[0];
    TestBed.configureTestingModule({
      providers: [
        provideNzIconsTesting(),
        provideNzIcons([{ name: 'icons:chevron-right', icon: '<svg viewBox="0 0 24 24"></svg>' }]),
        { provide: NZ_MODAL_DATA, useValue: { group } },
        { provide: NzModalRef, useValue: { close, updateConfig: vi.fn() } }
      ]
    });
    const fixture = TestBed.createComponent(StockLocationPickerModal);
    fixture.detectChanges();
    return { fixture, close };
  }

  it('lists every location', () => {
    const { fixture, close } = create();
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelectorAll('button').length).toBe(2);
    expect(element.textContent).toContain('Stoc redus');
    expect(close).not.toHaveBeenCalled();
    fixture.destroy();
  });

  it('closes with the location as soon as it is chosen', () => {
    const { fixture, close } = create();

    (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('button')[1].click();

    expect(close).toHaveBeenCalledWith(expect.objectContaining({ locationId: 'l2' }));
    fixture.destroy();
  });
});
