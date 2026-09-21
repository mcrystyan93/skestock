import { ComponentFixture, TestBed } from '@angular/core/testing';
import { type ItemAutocompleteValue } from '@ske/models';
import { By } from '@angular/platform-browser';
import { ItemAutocomplete } from '@ske/shared/items';
import { ItemsHttp } from '../../../../../items/services/items.http';
import { OrderListDetailForm } from './order-list-detail-form';

describe('OrderListDetailForm', () => {
  let fixture: ComponentFixture<OrderListDetailForm>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [OrderListDetailForm],
      providers: [
        {
          provide: ItemsHttp,
          useValue: {
            getAll: vi.fn(),
            getByIdCached: vi.fn()
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
      fixture.nativeElement.querySelector('input[placeholder="Cauta un articol"]').value
    ).toBe('');
  });
});
