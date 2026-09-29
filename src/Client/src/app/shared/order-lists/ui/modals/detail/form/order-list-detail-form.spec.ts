import { ComponentFixture, TestBed } from '@angular/core/testing';
import { type ItemAutocompleteValue } from '@ske/models';
import { By } from '@angular/platform-browser';
import { ItemAutocomplete } from '@ske/shared/items';
import { ItemsHttp } from '../../../../../items/services/items.http';
import { OrderListDetailForm } from './order-list-detail-form';
import { OrderListDetailState } from '../../../../services/order-list-detail.store';
import { OrderListsHttp } from '../../../../services/order-lists.http';
import { provideDispatcher } from '@ngrx/signals/events';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';

describe('OrderListDetailForm', () => {
  let fixture: ComponentFixture<OrderListDetailForm>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [OrderListDetailForm],
      providers: [
        provideDispatcher(),
        provideNzIconsTesting(),
        provideNzIcons([
          createTestIcon('icons:trash-can'),
          createTestIcon('icons:circle-exclamation'),
          createTestIcon('icons:circle-check'),
          createTestIcon('icons:magnifying-glass'),
          createTestIcon('icons:plus'),
          createTestIcon('icons:minus'),
          createTestIcon('icons:pencil')
        ]),
        OrderListDetailState,
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
      ]
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
    expect(fixture.componentInstance.orderListForm.lineItem().value()).toBeNull();
    expect(fixture.componentInstance.orderListForm.lineItem().controlValue()).toBeNull();
    expect(autocomplete.value()).toBeNull();
    expect(
      fixture.nativeElement.querySelector('input[placeholder="Cauta si adauga un articol"]').value
    ).toBe('');
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

function createLine() {
  return {
    id: null,
    itemId: 'item-1',
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
