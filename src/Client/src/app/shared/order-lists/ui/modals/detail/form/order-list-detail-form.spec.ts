import { EnvironmentProviders, Provider } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { type ItemAutocompleteValue } from '@ske/models';
import { By } from '@angular/platform-browser';
import { ItemAutocomplete } from '@ske/shared/items';
import { ItemsHttp } from '../../../../../items/services/items.http';
import { OrderListDetailForm } from './order-list-detail-form';
import { OrderListDetailState } from '../../../../services/order-list-detail.store';
import { OrderListsHttp } from '../../../../services/order-lists.http';
import { provideDispatcher } from '@ngrx/signals/events';
import { NzModalService } from 'ng-zorro-antd/modal';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { NzBreakpointService } from 'ng-zorro-antd/core/services';
import { of } from 'rxjs';

function providers(): (Provider | EnvironmentProviders)[] {
  return [
    provideDispatcher(),
    provideNzIconsTesting(),
    provideNzIcons([
      createTestIcon('icons:trash-can'),
      createTestIcon('icons:circle-exclamation'),
      createTestIcon('icons:circle-check'),
      createTestIcon('icons:magnifying-glass'),
      createTestIcon('icons:plus'),
      createTestIcon('icons:minus'),
      createTestIcon('icons:pencil'),
      createTestIcon('icons:clipboard-list'),
      createTestIcon('icons:box')
    ]),
    OrderListDetailState,
    { provide: NzModalService, useValue: { create: vi.fn() } },
    {
      provide: ItemsHttp,
      useValue: {
        getAll: vi.fn(),
        getByIdCached: vi.fn()
      }
    },
    {
      provide: OrderListsHttp,
      useValue: {
        getById: vi.fn(),
        create: vi.fn(),
        update: vi.fn()
      }
    }
  ];
}

describe('OrderListDetailForm', () => {
  let fixture: ComponentFixture<OrderListDetailForm>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [OrderListDetailForm],
      providers: providers()
    });

    fixture = TestBed.createComponent(OrderListDetailForm);
    fixture.componentRef.setInput('loading', false);
    fixture.componentRef.setInput('orderList', {
      id: null,
      name: 'Lista de test',
      note: null,
      status: 'Draft',
      lines: []
    });
    fixture.detectChanges();
  });

  it('clears the selected line item after adding it to the lines', () => {
    const lineItem: ItemAutocompleteValue = { name: 'Creion' };
    const autocomplete = fixture.debugElement.query(
      By.directive(ItemAutocomplete)
    ).componentInstance as ItemAutocomplete;

    autocomplete.value.set(lineItem);
    TestBed.tick();

    expect(fixture.componentInstance.orderListForm.lines().value()).toHaveLength(1);
    expect(fixture.componentInstance.orderListForm.lines().value()[0].itemImageFileId).toBeNull();
    expect(fixture.componentInstance.orderListForm.lineItem().value()).toBeNull();
    expect(fixture.componentInstance.orderListForm.lineItem().controlValue()).toBeNull();
    expect(autocomplete.value()).toBeNull();
    expect(
      fixture.nativeElement.querySelector('input[placeholder="Cauta si adauga un articol"]').value
    ).toBe('');
  });

  it('keeps the catalog item image on the added line', () => {
    const item = { id: 'item-1', name: 'Creion', sku: null, unit: 'buc', imageFileId: 'img-1' } as unknown as ItemAutocompleteValue;
    const autocomplete = fixture.debugElement.query(
      By.directive(ItemAutocomplete)
    ).componentInstance as ItemAutocomplete;

    autocomplete.value.set(item);
    TestBed.tick();

    expect(fixture.componentInstance.orderListForm.lines().value()[0].itemImageFileId).toBe('img-1');
  });

  it('does not add an item that is already in the list', () => {
    const item = { id: 'item-1', name: 'Creion', sku: null, unit: 'buc' } as unknown as ItemAutocompleteValue;
    const autocomplete = fixture.debugElement.query(
      By.directive(ItemAutocomplete)
    ).componentInstance as ItemAutocomplete;

    autocomplete.value.set(item);
    TestBed.tick();
    autocomplete.value.set(item);
    TestBed.tick();

    expect(fixture.componentInstance.orderListForm.lines().value()).toHaveLength(1);
    expect(fixture.componentInstance.orderListForm.lineItem().value()).toBeNull();
  });

  it('maps loaded lines with null fields to empty form values', () => {
    fixture.componentRef.setInput('orderList', {
      id: 'o1',
      status: 'Draft',
      lines: [{ id: 'l1', itemId: 'a', itemImageFileId: 'img-a', productName: 'A', quantity: 1, unit: 'buc', notes: null }]
    });
    fixture.detectChanges();

    expect(fixture.componentInstance.orderListForm.lines().value()[0].notes).toBe('');
    expect(fixture.componentInstance.orderListForm.lines().value()[0].itemImageFileId).toBe('img-a');
  });

  it('merges predefined list lines by item, adding quantities of items already listed', () => {
    const form = fixture.componentInstance;
    const line = (itemId: string, quantity: number) =>
      ({ id: null, itemId, productName: itemId, quantity, unit: 'buc', notes: '' });
    const addLines = (lines: ReturnType<typeof line>[]) =>
      (form as unknown as { addLines(l: unknown[]): void }).addLines(lines);

    addLines([line('a', 2)]);
    addLines([line('a', 3), line('b', 1)]);

    expect(form.orderListForm.lines().value().map((l) => [l.itemId, l.quantity])).toEqual([['b', 1], ['a', 5]]);
  });

  it('shows the collection validation message when no lines are present', async () => {
    const result = await fixture.componentInstance.submit();

    await fixture.whenStable();

    expect(result.isValid).toBe(false);
    expect(fixture.nativeElement.textContent).toContain(
      'Trebuie să adăugați cel puțin un articol în listă.'
    );
  });

  it('rejects non-positive line quantities', async () => {
    fixture.componentInstance.orderListForm.lines().value.set([createLine()]);
    fixture.componentInstance.orderListForm.lines().value.update(lines =>
      lines.map(line => ({ ...line, quantity: 0 }))
    );

    const result = await fixture.componentInstance.submit();

    await fixture.whenStable();

    expect(result.isValid).toBe(false);
    expect(fixture.nativeElement.textContent).toContain(
      'Cantitatea trebuie să fie mai mare decât 0.'
    );
  });

  it('shows line length validation messages', async () => {
    fixture.componentInstance.orderListForm.lines().value.set([createLine()]);
    fixture.componentInstance.orderListForm.lines().value.update(lines =>
      lines.map(line => ({
        ...line,
        unit: 'u'.repeat(51),
        notes: 'n'.repeat(1001)
      }))
    );

    const result = await fixture.componentInstance.submit();

    await fixture.whenStable();

    expect(result.isValid).toBe(false);
    expect(fixture.nativeElement.textContent).toContain(
      'Unitatea nu poate depăși 50 de caractere.'
    );
    expect(fixture.nativeElement.textContent).toContain(
      'Notițele articolului nu pot depăși 1000 de caractere.'
    );
  });

  it('renders status as read-only', () => {
    expect(fixture.componentInstance.statusLabel()).toBe('Ciornă');
    expect(fixture.nativeElement.querySelector('nz-select')).toBeNull();
  });

  it('disables editing controls for submitted order lists', () => {
    fixture.componentRef.setInput('orderList', {
      id: 'order-list-1',
      classId: 'class-1',
      name: 'Lista trimisă',
      note: null,
      status: 'Submitted',
      lines: []
    });
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector('input[placeholder="Numele comenzii"]').disabled
    ).toBe(true);
    expect(
      fixture.nativeElement.querySelector('input[placeholder="Cauta si adauga un articol"]').disabled
    ).toBe(true);
  });
});

describe('OrderListDetailForm collapsible details', () => {
  function create(md: boolean, name = 'Lista de test') {
    TestBed.configureTestingModule({
      imports: [OrderListDetailForm],
      providers: [...providers(), { provide: NzBreakpointService, useValue: { subscribe: () => of({ md }) } }]
    });
    const fixture = TestBed.createComponent(OrderListDetailForm);
    fixture.componentRef.setInput('loading', false);
    fixture.componentRef.setInput('orderList', { id: null, name, note: null, status: 'Draft', lines: [] });
    fixture.detectChanges();
    return fixture;
  }

  const header = (root: HTMLElement) => root.querySelector<HTMLElement>('.ant-collapse-header');
  const panel = (root: HTMLElement) => root.querySelector('.ant-collapse-item');

  it('renders the fields directly from md up', () => {
    const root = create(true).nativeElement as HTMLElement;

    expect(root.querySelector('nz-collapse')).toBeNull();
    expect(root.querySelector('input[placeholder="Numele comenzii"]')).not.toBeNull();
  });

  it('collapses the fields below md, showing the name in the header, and opens on click', () => {
    const fixture = create(false);
    const root = fixture.nativeElement as HTMLElement;

    expect(header(root)?.textContent).toContain('Lista de test');
    expect(panel(root)?.classList.contains('ant-collapse-item-active')).toBe(false);

    header(root)?.click();
    fixture.detectChanges();

    expect(panel(root)?.classList.contains('ant-collapse-item-active')).toBe(true);
  });

  it('stays open while the name is invalid', () => {
    const fixture = create(false, '');
    const root = fixture.nativeElement as HTMLElement;

    expect(panel(root)?.classList.contains('ant-collapse-item-active')).toBe(true);

    header(root)?.click();
    fixture.detectChanges();

    expect(panel(root)?.classList.contains('ant-collapse-item-active')).toBe(true);
  });
});

function createLine() {
  return {
    id: null,
    itemId: 'item-1',
    itemImageFileId: null,
    productName: 'Creion',
    quantity: 1,
    unit: 'buc',
    notes: '',
    clientKey: 'line-1'
  };
}

function createTestIcon(name: string) {
  return {
    name,
    icon: '<svg viewBox="0 0 1 1"><path d="M0 0h1v1H0z"/></svg>'
  };
}
