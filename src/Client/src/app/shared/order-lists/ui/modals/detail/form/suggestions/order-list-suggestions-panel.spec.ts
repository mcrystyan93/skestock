import { ComponentFixture, TestBed } from '@angular/core/testing';
import type { OrderCatalogItemDto, OrderListLineDto } from '@ske/models';
import { OrderListsHttp } from '@ske/shared/order-lists';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { Observable, of, throwError } from 'rxjs';
import { OrderListSuggestionsPanel } from './order-list-suggestions-panel';

const catalogItem = (itemId: string, name: string, lastQuantity: number | null, locationName = 'Dulap'): OrderCatalogItemDto => ({
  itemId,
  name,
  sku: null,
  imageFileId: null,
  unit: 'buc',
  lastQuantity,
  purchaseCount: 0,
  lastPurchasedAt: null,
  averageIntervalDays: null,
  isFrequent: false,
  locations: [{ locationId: locationName, locationName, quantity: 5, isLowStock: false }]
});

function createIcon(name: string) {
  return { name, icon: '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1 1"></svg>' };
}

describe('OrderListSuggestionsPanel', () => {
  let fixture: ComponentFixture<OrderListSuggestionsPanel>;
  let emitted: OrderListLineDto[];
  let getCatalog: ReturnType<typeof vi.fn>;

  async function create(catalog: Observable<OrderCatalogItemDto[]>, added: string[] = []) {
    getCatalog = vi.fn(() => catalog);
    TestBed.configureTestingModule({
      imports: [OrderListSuggestionsPanel],
      providers: [
        provideNzIconsTesting(),
        provideNzIcons(['magnifying-glass', 'plus', 'triangle-exclamation', 'arrows-rotate', 'box'].map((name) => createIcon(`icons:${name}`))),
        { provide: OrderListsHttp, useValue: { getCatalog } }
      ]
    });

    fixture = TestBed.createComponent(OrderListSuggestionsPanel);
    emitted = [];
    fixture.componentRef.setInput('classId', 'class-1');
    fixture.componentRef.setInput('addedItemIds', new Set(added));
    fixture.componentInstance.add.subscribe((line) => emitted.push(line));
    await fixture.whenStable();
    fixture.detectChanges();
  }

  const text = () => (fixture.nativeElement as HTMLElement).textContent ?? '';

  it('renders the catalog grouped by location without tabs', async () => {
    await create(of([catalogItem('a', 'Aţă', 2, 'Dulap'), catalogItem('b', 'Bec', 1, 'Raft')]));

    expect(getCatalog).toHaveBeenCalledWith('class-1');
    expect((fixture.nativeElement as HTMLElement).querySelector('.ant-tabs')).toBeNull();
    expect([...(fixture.nativeElement as HTMLElement).querySelectorAll('h4')].map((h) => h.textContent?.trim())).toEqual(['Dulap', 'Raft']);
  });

  it('adds a line with the last purchased quantity', async () => {
    await create(of([catalogItem('a', 'Aţă', 3.0)]));

    (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('button[aria-label="Adaugă Aţă"]')!.click();

    expect(emitted).toEqual([expect.objectContaining({ itemId: 'a', quantity: 3, unit: 'buc', productName: 'Aţă' })]);
  });

  it('marks rows already in the list', async () => {
    await create(of([catalogItem('a', 'Aţă', 3)]), ['a']);

    expect(text()).toContain('Adăugat');
    expect((fixture.nativeElement as HTMLElement).querySelector('button[aria-label="Adaugă Aţă"]')).toBeNull();
  });

  it('shows an error alert when the catalog fails', async () => {
    await create(throwError(() => new Error('boom')));

    expect(text()).toContain('Nu am putut încărca produsele.');
  });
});
