import { TestBed } from '@angular/core/testing';
import { type ItemDto } from '@ske/models';
import { of, Subject, throwError } from 'rxjs';
import { ItemDropdown } from './item-dropdown';
import { ItemsHttp } from '../../services/items.http';

describe('ItemDropdown', () => {
  const item: ItemDto = {
    id: 'item-1',
    name: 'Creion',
    sku: 'CR-1',
    description: null,
    unit: 'bucată',
    minThreshold: 1,
    isPerishable: false,
    shelfLifeDays: null,
    isActive: true,
    categoryId: 'category-1',
    categoryName: 'Papetărie',
    createdByName: null,
    lastModifiedByName: null,
    createdDate: '2026-01-01T00:00:00Z',
    lastModifiedDate: '2026-01-01T00:00:00Z'
  };

  let http: {
    getAll: ReturnType<typeof vi.fn>;
    getByIdCached: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    http = {
      getAll: vi.fn(),
      getByIdCached: vi.fn()
    };

    TestBed.configureTestingModule({
      imports: [ItemDropdown],
      providers: [{ provide: ItemsHttp, useValue: http }]
    });
  });

  it('shows a loading label before resolving an Id-only value', () => {
    const pendingItem = new Subject<ItemDto>();
    http.getByIdCached.mockReturnValue(pendingItem);

    const fixture = TestBed.createComponent(ItemDropdown);
    const dropdown = fixture.componentInstance;
    fixture.componentRef.setInput('allowEdit', false);
    fixture.componentRef.setInput('allowCreate', false);
    dropdown.value.set({ id: item.id });
    fixture.detectChanges();

    expect(dropdown.selectedItemLabel()).toBe('Se încarcă articolul…');
    expect(dropdown.selectedItemStatus()).toBe('Se încarcă articolul selectat.');

    pendingItem.next(item);
    pendingItem.complete();
    fixture.detectChanges();

    expect(dropdown.selectedItemLabel()).toBe(item.name);
    expect(dropdown.selectedItemStatus()).toBe('');
    fixture.destroy();
  });

  it('shows an explicit fallback when the Id cannot be resolved', () => {
    http.getByIdCached.mockReturnValue(throwError(() => ({ status: 404 })));

    const fixture = TestBed.createComponent(ItemDropdown);
    const dropdown = fixture.componentInstance;
    fixture.componentRef.setInput('allowEdit', false);
    fixture.componentRef.setInput('allowCreate', false);
    dropdown.value.set({ id: item.id });
    fixture.detectChanges();

    expect(dropdown.selectedItemLabel()).toBe(`Articol indisponibil (ID ${item.id})`);
    expect(dropdown.selectedItemStatus()).toBe(`Articolul cu ID ${item.id} nu este disponibil.`);
    fixture.destroy();
  });

  it('uses a supplied name without making a detail request', () => {
    http.getByIdCached.mockReturnValue(of(item));

    const fixture = TestBed.createComponent(ItemDropdown);
    const dropdown = fixture.componentInstance;
    fixture.componentRef.setInput('allowEdit', false);
    fixture.componentRef.setInput('allowCreate', false);
    dropdown.value.set(item);
    fixture.detectChanges();

    expect(dropdown.selectedItemLabel()).toBe(item.name);
    expect(http.getByIdCached).not.toHaveBeenCalled();
    fixture.destroy();
  });
});
