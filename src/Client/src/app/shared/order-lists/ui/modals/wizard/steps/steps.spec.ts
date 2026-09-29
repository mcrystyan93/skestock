import { TestBed } from '@angular/core/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { OrderListSourceStep } from './source-step';
import { OrderListSuggestionsStep } from './suggestions-step';
import { OrderListSupplyListStep } from './supply-list-step';

const icon = (name: string) => ({ name, icon: '<svg viewBox="0 0 1 1"><path d="M0 0h1v1H0z"/></svg>' });

describe('order list wizard steps', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideNzIconsTesting(),
        provideNzIcons([icon('icons:triangle-exclamation'), icon('icons:clipboard-list'), icon('icons:circle-check')])
      ]
    });
  });

  it('emits the chosen source', () => {
    const fixture = TestBed.createComponent(OrderListSourceStep);
    const emitted: string[] = [];
    fixture.componentInstance.sourceChange.subscribe((value) => emitted.push(value));
    fixture.detectChanges();

    const radios = fixture.nativeElement.querySelectorAll('input[type=radio]') as NodeListOf<HTMLInputElement>;
    radios[1].click();

    expect(radios).toHaveLength(2);
    expect(emitted).toEqual(['supplyList']);
  });

  it('shows the selection count and the location tags, and emits all ids on select-all', () => {
    const fixture = TestBed.createComponent(OrderListSuggestionsStep);
    const emitted: string[][] = [];
    fixture.componentInstance.selectedIdsChange.subscribe((ids) => emitted.push(ids));
    fixture.componentRef.setInput('items', [
      { itemId: '1', itemName: 'Hârtie', sku: null, unit: 'top', locations: ['A', 'B'] },
      { itemId: '2', itemName: 'Creion', sku: null, unit: 'buc', locations: ['A'] }
    ]);
    fixture.componentRef.setInput('selectedIds', ['1']);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('1 din 2 selectate');
    expect(fixture.nativeElement.querySelectorAll('nz-tag')).toHaveLength(3);

    (fixture.componentInstance as unknown as { toggleAll(checked: boolean): void }).toggleAll(true);
    TestBed.tick();
    expect(emitted).toEqual([['1', '2']]);
  });

  it('lists supply lists with their article count and emits every checked list', () => {
    const fixture = TestBed.createComponent(OrderListSupplyListStep);
    const emitted: string[][] = [];
    fixture.componentInstance.selectedIdsChange.subscribe((ids) => emitted.push(ids));
    fixture.componentRef.setInput('lists', [
      { id: 'a', name: 'Lista A', frequency: 'Monthly', isActive: true, lineCount: 1, createdDate: '', lastModifiedDate: '' },
      { id: 'b', name: 'Lista B', frequency: 'Weekly', isActive: true, lineCount: 25, createdDate: '', lastModifiedDate: '' }
    ]);
    fixture.componentRef.setInput('selectedIds', []);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('1 articol');
    expect(text).toContain('25 de articole');

    const boxes = fixture.nativeElement.querySelectorAll('input[type=checkbox]') as NodeListOf<HTMLInputElement>;
    boxes[0].click();
    fixture.detectChanges();
    boxes[1].click();
    fixture.detectChanges();

    expect(emitted.at(-1)).toEqual(['a', 'b']);
  });
});
