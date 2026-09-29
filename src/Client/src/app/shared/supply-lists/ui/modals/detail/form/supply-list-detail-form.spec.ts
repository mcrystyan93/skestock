import { ComponentFixture, TestBed } from '@angular/core/testing';
import { type SupplyListDto } from '@ske/models';
import { By } from '@angular/platform-browser';
import { ItemDropdown, ItemsHttp } from '@ske/shared/items';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { mapSupplyListSaveRequest } from '../supply-list-detail-modal';
import { SupplyListDetailForm } from './supply-list-detail-form';

describe('SupplyListDetailForm', () => {
  let fixture: ComponentFixture<SupplyListDetailForm>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [SupplyListDetailForm],
      providers: [
        provideNzIconsTesting(),
        provideNzIcons(['icons:trash-can', 'icons:pencil', 'icons:plus', 'icons:minus']
          .map((name) => ({ name, icon: '<svg viewBox="0 0 24 24"></svg>' }))),
        { provide: ItemsHttp, useValue: { getAll: vi.fn(), getByIdCached: vi.fn() } }
      ]
    });

    fixture = TestBed.createComponent(SupplyListDetailForm);
    fixture.componentRef.setInput('loading', false);
    fixture.componentRef.setInput('supplyList', {});
    fixture.detectChanges();
  });

  it('defaults a new list to weekly with no lines', () => {
    const value = fixture.componentInstance.supplyListForm().value();

    expect(value.frequency).toBe('Weekly');
    expect(value.lines).toEqual([]);
  });

  it('adds an item with quantity 1 and its unit, ignoring duplicates', () => {
    const form = fixture.componentInstance;

    form.addItem({ id: 'item-1', name: 'Hârtie', sku: 'H1', unit: 'top' });
    form.addItem({ id: 'item-1', name: 'Hârtie', sku: 'H1', unit: 'top' });

    expect(form.supplyListForm.lines().value()).toEqual([
      { itemId: 'item-1', itemName: '(H1) Hârtie', quantity: 1, unit: 'top', notes: '' }
    ]);
  });

  it('clears the dropdown after an item is selected', async () => {
    const dropdown = fixture.debugElement.query(By.directive(ItemDropdown)).componentInstance as ItemDropdown;

    dropdown.value.set({ id: 'item-1', name: 'Creion', unit: 'buc' });
    TestBed.tick();
    await Promise.resolve();
    fixture.detectChanges();
    TestBed.tick();

    expect(fixture.componentInstance.supplyListForm.lines().value()).toHaveLength(1);
    expect(fixture.componentInstance.supplyListForm.lineItem().value()).toBeNull();
    expect(dropdown.value()).toBeNull();
    expect(dropdown.itemForm.item().value()).toBeNull();
  });

  it('removes a line without orphan field errors', () => {
    const form = fixture.componentInstance;
    form.addItem({ id: 'item-1', name: 'Creion', unit: 'buc' } as never);
    form.addItem({ id: 'item-2', name: 'Pix', unit: 'buc' } as never);
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button[aria-label^="Elimină"]') as HTMLButtonElement;
    button.click();
    fixture.detectChanges();
    TestBed.tick();

    expect(form.supplyListForm.lines().value().map((l) => l.itemId)).toEqual(['item-1']);
  });

  it('requires a name', async () => {
    const result = await fixture.componentInstance.submit();

    expect(result.isValid).toBe(false);
  });

  it('requires a 2-52 week interval only for EveryXWeeks', async () => {
    const form = fixture.componentInstance.supplyListForm;
    form.name().value.set('Lista');
    form.frequency().value.set('EveryXWeeks');
    TestBed.tick();

    expect((await fixture.componentInstance.submit()).isValid).toBe(false);

    form.intervalWeeks().value.set(1);
    expect((await fixture.componentInstance.submit()).isValid).toBe(false);

    form.intervalWeeks().value.set(3);
    expect((await fixture.componentInstance.submit()).isValid).toBe(true);

    form.frequency().value.set('Monthly');
    form.intervalWeeks().value.set(null);
    expect((await fixture.componentInstance.submit()).isValid).toBe(true);
  });

  it('rejects non-positive quantities and long units', async () => {
    const form = fixture.componentInstance;
    form.supplyListForm.name().value.set('Lista');
    form.addItem({ id: 'item-1', name: 'Creion', unit: 'buc' });
    form.supplyListForm.lines().value.update((lines) =>
      lines.map((line) => ({ ...line, quantity: 0, unit: 'u'.repeat(51) })));

    const result = await form.submit();
    await fixture.whenStable();

    expect(result.isValid).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('Cantitatea trebuie să fie mai mare decât 0.');
    expect(fixture.nativeElement.textContent).toContain('Unitatea nu poate depăși 50 de caractere.');
  });

  it('is read-only for inactive lists', () => {
    fixture.componentRef.setInput('supplyList', createDto(false));
    fixture.detectChanges();

    const form = fixture.componentInstance.supplyListForm;
    expect(form.name().disabled()).toBe(true);
    expect(form.lines[0].quantity().disabled()).toBe(true);
    expect(fixture.nativeElement.querySelector('ske-item-dropdown')).toBeNull();
  });

  it('maps form data to a trimmed save request, dropping interval for non-EveryXWeeks', () => {
    const request = mapSupplyListSaveRequest({
      id: null,
      name: '  Lista  ',
      note: ' ',
      frequency: 'Weekly',
      intervalWeeks: 4,
      lines: [{ itemId: 'item-1', itemName: 'X', quantity: 2, unit: ' buc ', notes: '' }],
      lineItem: null
    });

    expect(request).toEqual({
      name: 'Lista',
      note: null,
      frequency: 'Weekly',
      intervalWeeks: null,
      lines: [{ itemId: 'item-1', quantity: 2, unit: 'buc', notes: null }]
    });
  });
});

function createDto(isActive: boolean): SupplyListDto {
  return {
    id: 'list-1',
    name: 'Lista',
    note: null,
    frequency: 'Weekly',
    intervalWeeks: null,
    isActive,
    lines: [{ id: 'l1', itemId: 'item-1', itemName: 'Creion', quantity: 1, unit: 'buc', notes: null }],
    createdDate: '2025-01-01T00:00:00Z',
    lastModifiedDate: '2025-01-01T00:00:00Z'
  };
}
