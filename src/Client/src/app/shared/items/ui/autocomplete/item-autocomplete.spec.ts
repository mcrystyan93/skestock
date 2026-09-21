import { ComponentFixture, TestBed } from '@angular/core/testing';
import { type ItemDto } from '@ske/models';
import { of, throwError } from 'rxjs';
import { ItemAutocomplete } from './item-autocomplete';
import { ItemsHttp } from '../../services/items.http';
import { type NzAutocompleteOptionComponent } from 'ng-zorro-antd/auto-complete';

describe('ItemAutocomplete', () => {
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
    lastModifiedDate: '2026-01-01T00:00:00Z',
  };

  let fixture: ComponentFixture<ItemAutocomplete>;
  let http: {
    getAll: ReturnType<typeof vi.fn>;
    getByIdCached: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    vi.useFakeTimers();
    http = {
      getAll: vi.fn().mockReturnValue(
        of({
          data: [],
          hasNextPage: false,
          nextCursor: null,
          sort: [],
        }),
      ),
      getByIdCached: vi.fn().mockReturnValue(of(item)),
    };

    TestBed.configureTestingModule({
      imports: [ItemAutocomplete],
      providers: [{ provide: ItemsHttp, useValue: http }],
    });
  });

  afterEach(() => {
    fixture?.destroy();
    vi.useRealTimers();
  });

  it('selects the single exact item when Enter is pressed', () => {
    const dropdown = createComponent();
    http.getAll.mockReturnValue(of(response([item])));

    search(dropdown, '  CREIÓN  ');
    dropdown.onEnter(new Event('keydown'));
    vi.runAllTimers();
    fixture.detectChanges();

    expect(dropdown.value()).toEqual(item);
    expect(http.getAll).toHaveBeenCalledWith(
      expect.objectContaining({
        searchTerm: 'creion',
      }),
    );
  });

  it('emits a trimmed draft when no item matches', () => {
    const dropdown = createComponent();

    search(dropdown, '  Pix nou  ');
    dropdown.onEnter(new Event('keydown'));
    vi.runAllTimers();
    fixture.detectChanges();

    expect(dropdown.value()).toEqual({ name: 'Pix nou' });
  });

  it('selects an item when its SKU is an exact match', () => {
    const dropdown = createComponent();
    http.getAll.mockReturnValue(of(response([item])));

    search(dropdown, item.sku!);
    dropdown.onEnter(new Event('keydown'));
    vi.runAllTimers();
    fixture.detectChanges();

    expect(dropdown.value()).toEqual(item);
  });

  it('requires an explicit result when multiple exact names match', () => {
    const dropdown = createComponent();
    const secondItem = { ...item, id: 'item-2', sku: 'CR-2' };
    http.getAll.mockReturnValue(of(response([item, secondItem])));

    search(dropdown, item.name);
    dropdown.onEnter(new Event('keydown'));
    vi.runAllTimers();
    fixture.detectChanges();

    expect(dropdown.value()).toBeNull();

    dropdown.onOptionSelected(optionWithValue(secondItem));

    expect(dropdown.value()).toEqual(secondItem);
  });

  it('allows creating a draft after a search error', () => {
    const dropdown = createComponent();
    http.getAll.mockReturnValue(throwError(() => ({ status: 500 })));

    search(dropdown, 'Articol nou');
    expect(dropdown.hasSearchError()).toBe(true);
    expect(dropdown.showCreateOption()).toBe(true);

    dropdown.onEnter(new Event('keydown'));
    vi.runAllTimers();
    fixture.detectChanges();

    expect(dropdown.value()).toEqual({ name: 'Articol nou' });
  });

  it('resolves an id-only initial value without changing the value shape', () => {
    const dropdown = createComponent();

    dropdown.value.set({ id: item.id });
    fixture.detectChanges();

    expect(http.getByIdCached).toHaveBeenCalledWith(item.id);
    expect(dropdown.selectedItemLabel()).toBe(item.name);
    expect(dropdown.value()).toEqual({ id: item.id });
  });

  it('loads another result page only for the active search', () => {
    const dropdown = createComponent();
    http.getAll.mockReturnValue(of(response([item], true, 'next-cursor')));

    search(dropdown, 'Cre');
    expect(dropdown.showLoadMoreOption()).toBe(true);

    dropdown.loadMore();

    expect(http.getAll).toHaveBeenCalledTimes(2);
    expect(http.getAll).toHaveBeenLastCalledWith(
      expect.objectContaining({
        cursor: 'next-cursor',
        searchTerm: 'cre',
      }),
    );
  });

  it('restores the committed value when a search is abandoned on blur', () => {
    const dropdown = createComponent();
    dropdown.value.set(item);
    fixture.detectChanges();
    dropdown.onFocus();
    dropdown.onInput(inputEvent('New text'));
    dropdown.onBlur();
    vi.runAllTimers();
    fixture.detectChanges();

    expect(dropdown.value()).toEqual(item);
    expect(dropdown.query()).toBe(item.name);
  });

  it('ignores input while disabled', () => {
    const dropdown = createComponent();
    fixture.componentRef.setInput('disabled', true);
    fixture.detectChanges();

    dropdown.onInput(inputEvent('Creion'));
    vi.advanceTimersByTime(300);

    expect(http.getAll).not.toHaveBeenCalled();
  });

  function createComponent() {
    fixture = TestBed.createComponent(ItemAutocomplete);
    fixture.detectChanges();
    return fixture.componentInstance;
  }

  function search(dropdown: ItemAutocomplete, value: string) {
    dropdown.onFocus();
    dropdown.onInput(inputEvent(value));
    vi.advanceTimersByTime(300);
    fixture.detectChanges();
  }

  function inputEvent(value: string): Event {
    const input = document.createElement('input');
    input.value = value;
    return { target: input } as unknown as Event;
  }

  function optionWithValue(value: ItemDto | symbol): NzAutocompleteOptionComponent {
    return { nzValue: value } as NzAutocompleteOptionComponent;
  }

  function response(data: ItemDto[], hasNextPage = false, nextCursor: string | null = null) {
    return {
      data,
      hasNextPage,
      nextCursor,
      sort: [],
    };
  }
});
