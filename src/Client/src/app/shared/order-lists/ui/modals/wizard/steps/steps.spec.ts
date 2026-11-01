import { TestBed } from '@angular/core/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { OrderListDetailsStep } from './details-step';
import { OrderListProductsStep } from './products-step';
import { OrderListSourceStep } from './source-step';
import { OrderListSuggestionsStep } from './suggestions-step';
import { OrderListSupplyListStep } from './supply-list-step';

const icon = (name: string) => ({ name, icon: '<svg viewBox="0 0 1 1"><path d="M0 0h1v1H0z"/></svg>' });

describe('order list wizard steps', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideNzIconsTesting(),
        provideNzIcons([icon('icons:triangle-exclamation'), icon('icons:clipboard-list'), icon('icons:circle-check'), icon('icons:box')])
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

  describe('segments step', () => {
    const seg = (itemId: string, name: string, averageQuantity: number) => ({
      itemId, name, sku: null, imageFileId: null, unit: 'buc', averageQuantity, purchaseCount: 5, averageIntervalDays: 7
    });

    function createSegments() {
      const fixture = TestBed.createComponent(OrderListSuggestionsStep);
      const emitted: string[][] = [];
      fixture.componentInstance.selectedIdsChange.subscribe((ids) => emitted.push(ids));
      fixture.componentRef.setInput('items', [
        { itemId: '1', itemName: 'Hârtie', sku: null, imageFileId: null, unit: 'top', locations: ['A', 'B'] },
        { itemId: '2', itemName: 'Creion', sku: null, imageFileId: null, unit: 'buc', locations: ['A'] }
      ]);
      fixture.componentRef.setInput('segments', {
        weekly: [seg('3', 'Lipici', 2.5)],
        everyTwoWeeks: [],
        monthly: [seg('4', 'Foarfecă', 1)],
        oncePerClass: []
      });
      fixture.componentRef.setInput('selectedIds', ['1', '4']);
      fixture.detectChanges();
      return { fixture, emitted, step: fixture.componentInstance as unknown as { toggleTab(tab: unknown, checked: boolean): void; tabs(): { key: string; ids: string[] }[] } };
    }

    it('renders five tabs with their counts', () => {
      const { fixture } = createSegments();
      const titles = [...fixture.nativeElement.querySelectorAll('.ant-tabs-tab')].map((tab) => (tab as HTMLElement).textContent?.replace(/\s+/g, ' ').trim());

      expect(titles).toEqual(['Stoc redus 2', 'Comandate săptămânal 1', 'La 2 săptămâni 0', 'Lunar 1', 'O dată pe clasă 0']);
    });

    it('groups low stock by location and repeats an item low in two places', () => {
      const { fixture } = createSegments();
      const root = fixture.nativeElement as HTMLElement;

      expect([...root.querySelectorAll('h4')].map((h) => h.textContent?.trim())).toEqual(['A', 'B']);
      expect(root.querySelectorAll('ske-item-thumbnail')).toHaveLength(3);
      expect(root.textContent).toContain('1 din 2 selectate');
    });

    it('select-all only touches the tab ids and keeps ticks made in other tabs', () => {
      const { emitted, step } = createSegments();
      const [lowStock, weekly] = step.tabs();

      step.toggleTab(weekly, true);
      expect(emitted.at(-1)).toEqual(['1', '4', '3']);

      step.toggleTab(lowStock, false);
      expect(emitted.at(-1)).toEqual(['4']);
    });

    it('shows the quantity of purchase rows and an empty state for tabs without data', () => {
      const { fixture, step } = createSegments();
      const root = fixture.nativeElement as HTMLElement;

      // Tabs render lazily; switch to the second one.
      (root.querySelectorAll('.ant-tabs-tab')[1] as HTMLElement).click();
      fixture.detectChanges();
      expect(root.textContent).toContain('2.5 buc');

      expect(step.tabs()[2].ids).toEqual([]);
    });
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

  it('emits the typed name and note', () => {
    const fixture = TestBed.createComponent(OrderListDetailsStep);
    const emitted: { name: string; note: string }[] = [];
    fixture.componentInstance.detailsChange.subscribe((value) => emitted.push(value));
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    input.value = 'Comandă nouă';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(emitted.at(-1)).toEqual({ name: 'Comandă nouă', note: '' });
  });

  it('shows each product with its lists and emits every id on select-all', () => {
    const fixture = TestBed.createComponent(OrderListProductsStep);
    const emitted: string[][] = [];
    fixture.componentInstance.selectedIdsChange.subscribe((ids) => emitted.push(ids));
    fixture.componentRef.setInput('products', [
      { itemId: '1', productName: 'Hârtie', imageFileId: null, quantity: 3, unit: 'top', listNames: ['A', 'B'] },
      { itemId: '2', productName: 'Creion', imageFileId: null, quantity: 1, unit: 'buc', listNames: ['A'] }
    ]);
    fixture.componentRef.setInput('selectedIds', ['1']);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('1 din 2 selectate');
    expect(fixture.nativeElement.querySelectorAll('ske-item-thumbnail')).toHaveLength(2);
    expect(fixture.nativeElement.querySelectorAll('nz-tag')).toHaveLength(3);

    (fixture.componentInstance as unknown as { toggleAll(checked: boolean): void }).toggleAll(true);
    TestBed.tick();
    expect(emitted).toEqual([['1', '2']]);
  });
});
