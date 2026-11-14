import { ComponentFixture, TestBed } from '@angular/core/testing';
import type { OrderCatalogItemDto } from '@ske/models';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { OrderCatalogList } from './order-catalog-list';

const icon = (name: string) => ({ name, icon: '<svg viewBox="0 0 1 1"><path d="M0 0h1v1H0z"/></svg>' });

const loc = (locationId: string, locationName: string, isLowStock = false) => ({ locationId, locationName, isLowStock });

const item = (itemId: string, name: string, overrides: Partial<OrderCatalogItemDto> = {}): OrderCatalogItemDto => ({
  itemId, name, sku: null, imageFileId: null, unit: 'buc', lastQuantity: 2, purchaseCount: 0, averageIntervalDays: null, isFrequent: false,
  locations: [loc('a', 'Cămară')], ...overrides
});

describe('OrderCatalogList', () => {
  let fixture: ComponentFixture<OrderCatalogList>;
  let emitted: string[][];
  let added: OrderCatalogItemDto[];

  async function create(items: OrderCatalogItemDto[], mode: 'select' | 'add', selected: string[] = [], addedIds: string[] = []) {
    TestBed.configureTestingModule({
      providers: [
        provideNzIconsTesting(),
        provideNzIcons([icon('icons:magnifying-glass'), icon('icons:triangle-exclamation'), icon('icons:arrows-rotate'), icon('icons:plus'), icon('icons:box')])
      ]
    });
    fixture = TestBed.createComponent(OrderCatalogList);
    emitted = [];
    added = [];
    fixture.componentRef.setInput('items', items);
    fixture.componentRef.setInput('mode', mode);
    fixture.componentRef.setInput('selectedIds', selected);
    fixture.componentRef.setInput('addedItemIds', new Set(addedIds));
    fixture.componentInstance.selectedIdsChange.subscribe((ids) => emitted.push(ids));
    fixture.componentInstance.add.subscribe((value) => added.push(value));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  const el = () => fixture.nativeElement as HTMLElement;
  const checkboxes = () => [...el().querySelectorAll('ske-order-catalog-row input[type=checkbox]')] as HTMLInputElement[];

  it('writes out what each indicator means next to its icon', async () => {
    await create([
      item('1', 'Făină', { locations: [loc('a', 'Cămară', true)], isFrequent: true }),
      item('2', 'Orez'),
      item('3', 'Sare', { lastQuantity: null })
    ], 'select');

    const text = (index: number) => el().querySelectorAll('ske-order-catalog-row')[index].textContent?.replace(/\s+/g, ' ');

    expect(text(0)).toContain('Stoc scăzut');
    expect(text(0)).toContain('Comandat frecvent');
    expect(text(0)).toContain('Ultima cantitate: 2 buc');
    expect(text(1)).not.toContain('Stoc scăzut');
    expect(text(1)).not.toContain('Comandat frecvent');
    expect(text(1)).toContain('Ultima cantitate: 2 buc');
    // Never ordered: no "last quantity" claim, just the default of 1.
    expect(text(2)).not.toContain('Ultima cantitate');
    expect(text(2)).toContain('Cantitate: 1 buc');
    expect(el().querySelectorAll('ske-order-catalog-row')[0].querySelectorAll('nz-icon[aria-hidden="true"]').length).toBeGreaterThanOrEqual(2);
  });

  it('emits the ticked id and shows an item in two groups checked in both', async () => {
    await create([item('1', 'Făină', { locations: [loc('a', 'Cămară'), loc('b', 'Bucătărie')] })], 'select', ['1']);

    expect(checkboxes().filter((box) => box.checked)).toHaveLength(2);

    checkboxes()[0].click();

    expect(emitted).toEqual([[]]);
  });

  it('selects all only in its own group, counting just the rows the search leaves', async () => {
    await create([
      item('1', 'Apă', { locations: [loc('a', 'Cămară')] }),
      item('2', 'Bere', { locations: [loc('a', 'Cămară')] }),
      item('3', 'Cafea', { locations: [loc('b', 'Bucătărie')] })
    ], 'select', ['3']);

    const headers = [...el().querySelectorAll('label[nz-checkbox]')].filter((l) => l.textContent?.includes('Selectează tot'));

    expect(headers.map((h) => h.textContent?.replace(/\s+/g, ' ').trim().replace(/ din .*/, ''))).toEqual([
      'Selectează tot (1)', 'Selectează tot (2)'
    ]);

    (headers[1].querySelector('input') as HTMLInputElement).click();

    expect(emitted).toEqual([['3', '1', '2']]);
  });

  it('filters by the search text', async () => {
    await create([item('1', 'Pâine'), item('2', 'Lapte')], 'select');

    const search = el().querySelector('input[type=search]') as HTMLInputElement;
    search.value = 'paine';
    search.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(el().querySelectorAll('ske-order-catalog-row')).toHaveLength(1);
    // The search sits outside the scrolling area and each group heading is pinned inside it.
    expect(el().querySelector('.overflow-y-auto input[type=search]')).toBeNull();
    expect(el().querySelector('.overflow-y-auto section h4')?.parentElement?.classList).toContain('sticky');
  });

  it('add mode emits the item and shows "Adăugat" for added ids', async () => {
    await create([item('1', 'Apă'), item('2', 'Bere')], 'add', [], ['2']);

    const button = el().querySelector('button[aria-label="Adaugă Apă"]') as HTMLButtonElement;
    button.click();

    expect(added.map((value) => value.itemId)).toEqual(['1']);
    expect(el().textContent).toContain('Adăugat');
    expect(el().querySelector('[aria-label="Adaugă Bere"]')).toBeNull();
    expect(el().querySelector('label[nz-checkbox]')).toBeNull();
  });

  it('shows the empty text without a catalog', async () => {
    await create([], 'select');

    expect(el().textContent).toContain('Nu există produse active.');
  });

  it('shows "Niciun rezultat." when the search matches nothing', async () => {
    await create([item('1', 'Pâine')], 'select');

    const search = el().querySelector('input[type=search]') as HTMLInputElement;
    search.value = 'zzz';
    search.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(el().textContent).toContain('Niciun rezultat.');
  });
});
