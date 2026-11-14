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
        provideNzIcons([icon('icons:triangle-exclamation'), icon('icons:arrows-rotate'), icon('icons:magnifying-glass'), icon('icons:clipboard-list'), icon('icons:circle-check'), icon('icons:box')])
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

  describe('catalog step', () => {
    const locations = [{ locationId: 'a', locationName: 'Cămară', quantity: 5, isLowStock: false }];
    const catalogItem = (itemId: string, name: string) => ({
      itemId, name, sku: null, imageFileId: null, unit: 'buc', lastQuantity: 2, purchaseCount: 0, lastPurchasedAt: null, averageIntervalDays: null, isFrequent: false, locations
    });

    async function createCatalog(loading = false) {
      const fixture = TestBed.createComponent(OrderListSuggestionsStep);
      const emitted: string[][] = [];
      fixture.componentInstance.selectedIdsChange.subscribe((ids) => emitted.push(ids));
      fixture.componentRef.setInput('items', [catalogItem('1', 'Hârtie'), catalogItem('2', 'Creion')]);
      fixture.componentRef.setInput('selectedIds', ['1']);
      fixture.componentRef.setInput('loading', loading);
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();
      return { fixture, emitted };
    }

    it('shows one list without tabs and the selected count', async () => {
      const { fixture } = await createCatalog();
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('.ant-tabs')).toBeNull();
      expect(root.querySelectorAll('ske-order-catalog-row')).toHaveLength(2);
      expect(root.textContent).toContain('1 selectate');
    });

    it('forwards a tick from the list', async () => {
      const { fixture, emitted } = await createCatalog();

      (fixture.nativeElement.querySelectorAll('ske-order-catalog-row input[type=checkbox]')[0] as HTMLInputElement).click();

      expect(emitted).toEqual([['1', '2']]);
    });

    it('shows a skeleton instead of the list while loading', async () => {
      const { fixture } = await createCatalog(true);

      expect(fixture.nativeElement.querySelector('ske-order-catalog-list')).toBeNull();
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
