import { TestBed } from '@angular/core/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { groupStockByItem, StockItemDto } from '@ske/models';
import { StockItemActions } from './stock-item-actions';

describe('StockItemActions', () => {
  const line = (locationId: string): StockItemDto => ({
    itemId: 'item-1', itemName: 'Mere', categoryId: 'c', categoryName: 'Alimente', locationId, locationName: locationId,
    lastUpdatedAt: null, unit: 'kg', isPerishable: false, isExpired: false, expiredQuantity: 0, quantity: 4,
    isLowStock: false, hideWhenZeroStock: false
  });

  function create(locations: string[]) {
    TestBed.configureTestingModule({
      providers: [provideNzIconsTesting(), provideNzIcons(['ellipsis', 'arrow-right-arrow-left', 'cart-plus', 'calendar-plus', 'trash', 'eye', 'eye-slash']
        .map(name => ({ name: `icons:${name}`, icon: '<svg viewBox="0 0 24 24"></svg>' })))]
    });
    const fixture = TestBed.createComponent(StockItemActions);
    fixture.componentRef.setInput('group', groupStockByItem(locations.map(line))[0]);
    fixture.detectChanges();
    return fixture;
  }

  it('does not create the dropdown until the menu is first opened', () => {
    const fixture = create(['l1']);
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('[nz-dropdown]')).toBeNull();

    element.querySelector<HTMLButtonElement>('button[aria-label="Acțiuni pentru Mere"]')!.click();
    fixture.detectChanges();

    expect(element.querySelector('[nz-dropdown]')).not.toBeNull();
    expect(element.querySelector('button')!.getAttribute('aria-expanded')).toBe('true');
    fixture.destroy();
  });
});
