import { TestBed } from '@angular/core/testing';
import { StockItemCategoryGroup, StockItemDto } from '@ske/models';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { StockCategoryItemsSmall } from './stock-category-items-small';

describe('StockCategoryItemsSmall', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [StockCategoryItemsSmall],
      providers: [
        provideNzIconsTesting(),
        provideNzIcons([
          { name: 'icons:box', icon: '<svg viewBox="0 0 24 24"></svg>' },
          { name: 'icons:plus', icon: '<svg viewBox="0 0 24 24"></svg>' },
          { name: 'icons:clock', icon: '<svg viewBox="0 0 24 24"></svg>' },
          { name: 'icons:ellipsis-vertical', icon: '<svg viewBox="0 0 24 24"></svg>' },
          { name: 'icons:arrow-right-arrow-left', icon: '<svg viewBox="0 0 24 24"></svg>' },
          { name: 'icons:trash', icon: '<svg viewBox="0 0 24 24"></svg>' },
          { name: 'icons:cart-plus', icon: '<svg viewBox="0 0 24 24"></svg>' },
          { name: 'icons:calendar-plus', icon: '<svg viewBox="0 0 24 24"></svg>' },
          { name: 'icons:eye', icon: '<svg viewBox="0 0 24 24"></svg>' },
          { name: 'icons:eye-slash', icon: '<svg viewBox="0 0 24 24"></svg>' }
        ])
      ]
    });
  });

  it('shows skeleton cards while the stock list is loading', () => {
    const fixture = createFixture([], true);

    expect((fixture.nativeElement as HTMLElement).querySelectorAll('nz-skeleton').length).toBe(4);
    expect((fixture.nativeElement as HTMLElement).querySelector('nz-empty')).toBeNull();
    fixture.destroy();
  });

  it('shows an empty state when the class has no stock', () => {
    const fixture = createFixture();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Nu există stoc');
    fixture.destroy();
  });

  it('emits category add and item adjustment actions', () => {
    const item = createStockItem();
    const fixture = createFixture([createCategory([item])]);
    const component = fixture.componentInstance;
    const add = vi.spyOn(component.add, 'emit');
    const adjust = vi.spyOn(component.adjust, 'emit');
    const element = fixture.nativeElement as HTMLElement;

    element.querySelector<HTMLButtonElement>('button[aria-label="Adaugă stoc pentru Papetărie"]')!.click();
    element.querySelector<HTMLButtonElement>('button[aria-label^="Ajustează stocul pentru Caiet"]')!.click();

    expect(add).toHaveBeenCalledWith({ id: 'category-1', name: 'Papetărie' });
    expect(adjust).toHaveBeenCalledWith(item);
    fixture.destroy();
  });

  it('applies low-stock tone in preference to expired tone', () => {
    const fixture = createFixture();
    const component = fixture.componentInstance;

    expect(component.cardToneClass(createStockItem({ isLowStock: true, isExpired: true }))).toBe('!bg-amber-500/10');
    expect(component.cardToneClass(createStockItem({ isExpired: true }))).toBe('!bg-red-500/15');
    fixture.destroy();
  });

  function createFixture(items: StockItemCategoryGroup[] = [], loading = false) {
    const fixture = TestBed.createComponent(StockCategoryItemsSmall);
    fixture.componentRef.setInput('items', items);
    fixture.componentRef.setInput('loading', loading);
    fixture.detectChanges();
    return fixture;
  }
});

function createCategory(items: StockItemDto[]): StockItemCategoryGroup {
  return {
    categoryId: 'category-1',
    categoryName: 'Papetărie',
    items,
    item: null,
    isHeader: true,
    trackKey: 'category-1_header'
  };
}

function createStockItem(overrides: Partial<StockItemDto> = {}): StockItemDto {
  return {
    itemId: 'item-1',
    itemName: 'Caiet dictando',
    categoryId: 'category-1',
    categoryName: 'Papetărie',
    locationId: 'location-1',
    locationName: 'Sala 1',
    lastUpdatedAt: '2026-09-28T10:00:00Z',
    unit: 'buc',
    isPerishable: false,
    isExpired: false,
    expiredQuantity: 0,
    quantity: 12,
    isLowStock: false,
    hideWhenZeroStock: false,
    ...overrides
  };
}
