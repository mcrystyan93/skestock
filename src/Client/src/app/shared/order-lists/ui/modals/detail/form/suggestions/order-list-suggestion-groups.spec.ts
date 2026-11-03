import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { OrderListSuggestionGroups, type SuggestionGroup } from './order-list-suggestion-groups';

const row = (itemId: string, productName: string) => ({
  itemId,
  line: { id: null, itemId, productName, quantity: 1, unit: 'buc', notes: '' },
  detail: '1 buc'
});

const GROUPS: SuggestionGroup[] = [
  { title: 'Săptămânal', rows: [row('a', 'Pâine'), row('b', '(PX-1) Pix albastru')] },
  { title: 'Lunar', rows: [row('c', 'Săpun')] }
];

function createIcon(name: string) {
  return { name, icon: '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1 1"></svg>' };
}

describe('OrderListSuggestionGroups', () => {
  let fixture: ComponentFixture<OrderListSuggestionGroups>;

  function create(groups: SuggestionGroup[]) {
    TestBed.configureTestingModule({
      imports: [OrderListSuggestionGroups],
      providers: [provideNzIconsTesting(), provideNzIcons([createIcon('icons:magnifying-glass'), createIcon('icons:chevrons-right'), createIcon('icons:box')])]
    });
    fixture = TestBed.createComponent(OrderListSuggestionGroups);
    fixture.componentRef.setInput('groups', groups);
    fixture.componentRef.setInput('addedItemIds', new Set<string>());
    fixture.componentRef.setInput('emptyText', 'Nimic aici');
    fixture.detectChanges();
  }

  const root = () => fixture.nativeElement as HTMLElement;
  const text = () => root().textContent ?? '';

  function search(query: string) {
    const input = root().querySelector<HTMLInputElement>('input')!;

    input.value = query;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  it('shows every group and row without a query', () => {
    create(GROUPS);

    expect(root().querySelectorAll('li')).toHaveLength(3);
    expect(text()).toContain('Săptămânal');
    expect(text()).toContain('Lunar');
  });

  it('matches the product name ignoring case and diacritics, keeping the group name', () => {
    create(GROUPS);
    search('PAINE');

    expect(root().querySelectorAll('li')).toHaveLength(1);
    expect(text()).toContain('Pâine');
    expect(text()).toContain('Săptămânal');
    expect(text()).not.toContain('Lunar');
  });

  it('matches the SKU', () => {
    create(GROUPS);
    search('px-1');

    expect(text()).toContain('Pix albastru');
    expect(root().querySelectorAll('li')).toHaveLength(1);
  });

  it('keeps a whole group when its name matches', () => {
    create(GROUPS);
    search('saptamanal');

    expect(root().querySelectorAll('li')).toHaveLength(2);
  });

  it('says so when nothing matches', () => {
    create(GROUPS);
    search('zzz');

    expect(text()).toContain('Niciun rezultat.');
  });

  it('hides the search box and shows the empty text when there are no groups', () => {
    create([]);

    expect(root().querySelector('input')).toBeNull();
    expect(text()).toContain('Nimic aici');
  });
});
